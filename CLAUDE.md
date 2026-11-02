# Dotnetstore.PersonalApp — Avalonia Modular Monolith

## Project Context

.NET 10 **Avalonia desktop application** (MVVM) structured as a modular monolith. Modules live in one process (the Host app) but keep strict boundaries: each module owns its features (views, view models, handlers), data, and domain logic. Modules communicate only through integration events (Wolverine in-process), never by calling each other's handlers or sharing tables. The domain is CRUD-heavy and the team is solo, so each module uses **Vertical Slice Architecture** internally. There is no HTTP API and no minimal API.

## Tech Stack

- **.NET 10** / C# 14 (`Directory.Build.props`: nullable, `AnalysisLevel=latest-all`, warnings as errors)
- **Avalonia 12.0.x** with Fluent theme, compiled bindings on by default (`x:DataType` required in every `.axaml`)
- **CommunityToolkit.Mvvm** — `[ObservableProperty]`, `[RelayCommand]`
- **Microsoft.Extensions.DependencyInjection** — composition root in `Host/CompositionRoot.cs`; each module exposes `Add<Module>Module(...)`
- **EF Core + SQLite** — one DbContext per module, one database file/schema per module
- **Wolverine** — in-process integration events between modules, outbox where persistence is involved
- **HybridCache** (+ Redis if distributed caching is ever needed)
- **Auth** — none (desktop app; revisit if a remote backend is added)
- **FluentValidation**, **Serilog**
- **xUnit v3** (+ `Avalonia.Headless.XUnit` for UI tests); assertions use **Shouldly** (`x.ShouldBe(...)`), never `Assert.*`
- Central package management: add versions to `Directory.Packages.props`, reference without a version in the csproj

## Architecture

```
src/
  Dotnetstore.PersonalApp.Host/        # Avalonia app: App.axaml, Program.cs, MainWindow, shell navigation, CompositionRoot
  Dotnetstore.PersonalApp.Shared/      # Shared kernel (thin): integration event contracts, Result, primitives
  Modules/
    <Module>/Dotnetstore.PersonalApp.Modules.<Module>/
      Features/<Feature>/
        <Feature>View.axaml(.cs)         # Avalonia view
        <Feature>ViewModel.cs            # MVVM view model
        <Operation>.cs                   # Command/Query + Handler + Response
      Persistence/<Module>DbContext.cs
      Consumers/                         # Wolverine handlers for integration events
      <Module>Module.cs                  # public Add<Module>Module(...) registration
tests/
  Dotnetstore.PersonalApp.Integration.Tests/   # Composition root + cross-module tests
  Modules/Dotnetstore.PersonalApp.Modules.<Module>.Tests/
```

Modules so far: **Clients** (CRUD for companies, unique normalized organization number, SQLite `clients.db`), **Users** (CRUD, SQLite `users.db`), plus Addresses, EmailAddresses, PhoneNumbers and TimeReports described below; all databases live under `%LOCALAPPDATA%/Dotnetstore.PersonalApp`. Add more with `/scaffold`.

- **Photos**: users and clients can have a photo (PNG/JPEG, max 6 MB, stored as `Photo` bytes in the owning module's database; `Get<X>Photo` / `Set<X>Photo` handlers read and write only that column so lists and updates never load it). The shared `PhotoPanelViewModel` / `PhotoPanelView` (`Shared/Photos`, with `PhotoRules` and `BytesToBitmapConverter`) draw the round photo or initials with Change/Remove buttons on the User and Client detail pages; each module hands it load/save delegates through its `<X>PhotoPanelFactory`, and the file dialog is `IFileOpenService`. Error codes are `photo_too_large` / `photo_invalid` (shared strings). Cvs keeps its own per-CV photo. Contact persons have photos the same way. Every saved photo also gets a 96x96 PNG `PhotoThumbnail` (`PhotoThumbnail.Create`, SkiaSharp, made by the `Set<X>Photo` handler; bytes that cannot be decoded are `photo_invalid`); list queries and DTOs carry only that thumbnail (`Thumbnail`), shown by the shared `AvatarView` in the Users, Clients and Contact persons lists, the sidebar and the top bar. The signed-in user's picture reaches the shell through `IUserPhotoLookup` (`Shared/Users`, implemented by Users); `SetUserPhoto` publishes `UserPhotoChanged`, which the Host's `UserPhotoNotifier` turns into a refresh of the open shell. Photos saved before thumbnails existed show initials in lists until they are uploaded again.
- **Clients contact persons**: a client has any number of `ContactPerson` (`ClientId` FK with cascade, first/last name, optional job title and notes; error codes `contact_person_*`), managed in a Contact persons tab on the Client detail page. A person's detail page reuses the Addresses / Email addresses / Phone numbers panel factories with the person's id as `ObjectId`, so those modules need no schema change. Deleting a person publishes `ContactPersonDeleted`; deleting a client publishes one per person before `ClientDeleted`; Addresses, EmailAddresses and PhoneNumbers each have a `ContactPersonDeleted` consumer that removes the person's entries.
- **Addresses** (SQLite `addresses.db`) owns addresses for every other module. Other modules never reference it or share its tables: an `Address` points at its owner by a single `ObjectId` (user/client/customer ids are globally unique GUIDs, so no owner type is stored), and cleanup on owner deletion will go through integration events. So far only **AddressType** (user-managed lookup: unique case-insensitive `Name`, optional `Description`; `Billing`, `Delivery`, `Visiting`, `Home` are seeded once by the migration via `HasData` and are then editable/deletable). `Address` (ObjectId, AddressTypeId, CareOf, Street1/2, PostalCode, City, State, Country) listing is always per `ObjectId`; `DeleteAddressesForObject` removes all of an object's addresses. Other modules show an object's addresses through `IObjectAddressesPanelFactory` (in `Shared/Addresses`, implemented by Addresses): the Users and Clients detail pages each have an Addresses tab built from `factory.Create(id)` (the factory is optional, so the tab disappears if the module is absent). Address types have their own CRUD page in the sidebar. A type in use cannot be deleted (`address_type_in_use`).
- **EmailAddresses** (SQLite `emailaddresses.db`) is the email counterpart of Addresses and follows the same design: user-managed `EmailAddressType` (seeded `Work`, `Private`, `Billing`), `EmailAddress` (`ObjectId`, type, `Email`; unique per object, case-insensitive), `IObjectEmailAddressesPanelFactory` for the Email addresses tab on the Users and Clients detail pages, and consumers of `UserDeleted` / `ClientDeleted` that delete the owner's email addresses. Error codes are `email_address_*` / `email_address_type_*`.
- **PhoneNumbers** (SQLite `phonenumbers.db`) follows the same design again: user-managed `PhoneNumberType` (seeded `Mobile`, `Work`, `Home`), `PhoneNumber` (`ObjectId`, type, `Number` as typed plus a `NormalizedNumber` of optional `+` and digits; 5-15 digits; unique per object by normalized value so `070-123 45 67` and `0701234567` clash), `IObjectPhoneNumbersPanelFactory` for the Phone numbers tab on Users and Clients, and `UserDeleted` / `ClientDeleted` consumers. Error codes are `phone_number_*` / `phone_number_type_*`.
- **Cvs** (SQLite `cvs.db`) lets each user keep several CVs aimed at different jobs: `Cv` (`UserId`, unique case-insensitive `Title` per user, optional `TargetJob`, `Summary`). Handlers are scoped by `UserId`; the module has its own sidebar page (signed-in user) and a CVs tab on the User detail page via `IUserCvsPanelFactory` (`Shared/Cvs`). A `UserDeleted` consumer deletes the user's CVs. Error codes are `cv_*`. Each CV opens a read-only detail page (owner details from `IUserLookup` in `Shared/Users`, photo, summary, work experience, education, courses, languages, driver licence); entries are changed in an editor that replaces the overview. The photo is stored per CV (PNG/JPEG, max 6 MB) and picked through `IFileOpenService` (`Shared/Files`, dialog in the Host). Contact details are the user's own data from Addresses, PhoneNumbers and EmailAddresses, read through `IContactInfoSource` (`Shared/Contacts`, one implementation per module, so Cvs never references them); each CV stores only which entry ids to show (`CvContactSelection`, ids of other modules so no foreign key; `SetCvContacts` ignores ids that are not the user's, deleted ones just disappear) plus an optional `LinkedIn` URL (normalised to https, must be on linkedin.com, error `cv_linkedin_invalid`). A CV can be exported to Word (`ExportCvToWord`, Open XML SDK, in the current UI language) and handed to the user through `IFileSaveService`.
- **TimeReports** (SQLite `timereports.db`) lets the signed-in user report time per client: `TimeReport` (`UserId`, `ClientId`, `Date`, `StartTime`/`EndTime`, `TotalHours` typed by hand and not derived from start/end, optional `Location`, `DrivingKilometers`). Reports are per user (every handler is scoped by `UserId`, taken from `ICurrentUser` in `Shared/Auth`, which the Host sets on sign-in and clears on sign-out). Client names come from `IClientLookup` (`Shared/Clients`, implemented by Clients), so the module never references Clients. The module has its own sidebar page (client filter, totals) and a Time reports tab on the Client detail page via `ITimeReportsPanelFactory` (`Shared/TimeReports`). Every report has a `WorkTimeTypeId`: **WorkTimeType** is a user-managed lookup in the same module/database (unique case-insensitive `Name`, optional `Description`; `Work`, `Sick`, `Vacation`, `Parental leave` seeded once via `HasData`, then editable/deletable), with its own CRUD page in the sidebar; a type in use cannot be deleted (`work_time_type_in_use`). Error codes are `time_report_*` / `work_time_type_*`. The list can be exported to Excel (ClosedXML, `ExportTimeReports`): one client + one calendar year, one sheet per month with reports named `yyyy-MM`; a second export (`ExportKilometerSummary`) gives one sheet for a year with driving km per client (rows) and month (columns), row/column totals and the year total for tax purposes; the file is handed to the user through `IFileSaveService` (`Shared/Files`, save dialog implemented in the Host). The yearly export can be emailed (`SendTimeReportViewModel`, shown in the editor slot): the recipient is the client or one of its contact persons (`IContactPersonLookup` in `Shared/Clients`, implemented by Clients), the address is chosen among the email addresses registered for that recipient (read through every `IContactInfoSource`, kind Email; picked automatically when there is only one), and the mail goes out through `ISystemMailSender` (`Shared/Email`, implemented by Email), which always uses the system SMTP account (first by name; error `email_no_system_smtp` if none). The Email button is hidden when the Email module is absent. Reports are deliberately kept when a client or user is deleted (billing records; the list then shows no client name). Rules: end after start (no overnight), hours 0-24, km 0-10000. Error codes are `time_report_*`.
- **Email** (SQLite `email.db`) holds the mail accounts and uses them to send and receive mail. All server access goes through `IMailGateway` (`Mail/`, the only MailKit user, which sorts failures into `email_authentication_failed` / `email_tls_failed` / `email_connection_failed` / `email_server_error`; tests use a fake). `TestEmailConnection` tries the editor's settings (an empty password on an existing account falls back to the stored one). `SendEmail` sends text (or HTML, see below) through an SMTP account the user may use, from the account's address. `FetchEmail` downloads up to 50 of the newest new messages of a POP3 account into `ReceivedMessage` (unique per account + UIDL, body cut at 100 000 characters, plain text part kept, HTML kept separately) and never deletes on the server; messages belong to the account, so a system inbox is shared and they go with the account (cascade). `SendEmail` sends text or HTML with attachments (`IFilePickService` in `Shared/Files`, dialog in the Host) and keeps a `SentMessage` copy after the server accepted it. Attachments (`MailAttachment`, bytes in the database, max 20 per message, 10 MB each, 25 MB together when sending; larger received ones are skipped) belong to a received or a sent message and are saved through `IFileSaveService`. Deleting a received message is local only: its UIDL goes into `DeletedMessage` so later fetches skip it; sent copies can be deleted too. Reply and forward open an embedded `ComposeViewModel` in the inbox (forward keeps attachments). The inbox view runs a `DispatcherTimer` that calls `AutoRefreshCommand` every 5 minutes; the view model decides whether to fetch (checkbox, not busy) and stays quiet unless something new arrived or it failed. Received HTML is stored (`HtmlBody`, cut at 200 000 characters) and shown formatted by `HtmlFormatter` (MimeKit tokenizer to blocks of styled runs, drawn by `FormattedBodyPanel`): paragraphs, headings, lists, quotes, bold/italic/underline; scripts, styles, forms and frames are dropped, images are never loaded (shown as their alt text) and links are drawn but not clickable (address in a tooltip); the inbox offers a Show plain text toggle. Messages carry `To`/`Cc` recipients (`SendEmail` takes a `Cc`, removes Cc addresses already in To, and limits To plus Cc together to 50); the inbox has Reply all (sender + other To recipients as To, original Cc as Cc, our own address and repeats left out; mail fetched before recipients were stored replies to the sender only). Drafts (`Draft`, `SaveDraft`/`ListDrafts`/`GetDraft`/`DeleteDraft`, error codes `email_draft_*`) are personal to the user even when written for a system account, keep their attachments, are deleted when sent and with the user, and have a Drafts page in the Email group; the composer has a Save draft button, and saving replaces attachments inside one transaction. A stored mail password is only reused (test connection, update with an empty password) while host, port and username are unchanged, otherwise `email_account_password_required`. UIDLs longer than 200 characters are stored as `sha256:` + hash (`MailLimits.NormalizeUid`) and compared that way. Failing to keep the sent copy after the server accepted the mail is ignored so a retry does not send twice. The inbox lists 50 messages and loads older ones on request; an auto-refresh keeps the open message. `IFilePickService` returns the unreadable or too large files as `Skipped` so the composer can say so. Sidebar group "Email" with Inbox, New message, Sent mail and Email accounts pages (`ModuleKey` `email`). Account details: `EmailAccount` is either SMTP or POP3 (`Protocol`), with `Host`, `Port`, `Security` (None / StartTls / SslOnConnect), `EmailAddress`, optional `Username` and a password stored only as `ProtectedPassword` (Windows DPAPI through `ISecretProtector`, so the module only runs on Windows; handlers and DTOs never return it, an update with a null password keeps the stored one). An account is personal (`OwnerId` = the user) or a system account shared by everyone (`OwnerId` = `EmailAccount.SystemOwner`, an empty Guid so the unique case-insensitive `(OwnerId, Name)` index covers it). A user sees and may change their own and the system accounts, never another user's; there are no roles, so any signed-in user can add, change or delete system accounts, and the scope cannot be changed after creation. A `UserDeleted` consumer deletes the user's personal accounts only. Own sidebar page "Email accounts" (`ModuleKey` `email`). Error codes are `email_account_*`.
- **Integration events** currently use a minimal in-process dispatcher, not Wolverine (which needs a generic host this app does not have): contracts `IEventPublisher` / `IIntegrationEventConsumer<TEvent>` in `Shared/Events`, event records in `Shared/Contracts/Events`, implementation `InProcessEventPublisher` in the Host. A module publishes after its own change succeeds; consumers live in `Consumers/` and are registered as `IIntegrationEventConsumer<T>`. Delivery is synchronous with no outbox, so a crash between the delete and the consumer leaves orphans. Today: `UserDeleted` / `ClientDeleted` make Addresses delete the owner's addresses.
- Error codes are prefixed per module (`user_*`, `client_*`) so resource keys `Error_{code}` never collide across modules.
- `AddClientsModule` is registered before `AddUsersModule` in `CompositionRoot`, so Clients is the first nav page (the landing page).
- Strings needed by several modules (e.g. `Common_Cancel`) live in `Shared/Resources/SharedStrings*.resx`, not in a module.

### Module conventions (from Users)

- Handlers are `internal static class <Op>` wrappers holding `Command`/`Query`, optional `Validator`, and a `Handler` that takes `IDbContextFactory<TDbContext>` (a context per operation, since view models are long-lived) and returns `Result<T>` from `Shared.Results`.
- A module exposes `Add<Module>Module(this IServiceCollection, ...)`, registers an `IModulePage` for shell navigation and an `IStartupTask` that applies its EF migrations.
- Passwords are hashed with `IPasswordHasher<T>` (`Microsoft.Extensions.Identity.Core`); never store or log plain text.
- Migrations live in `Persistence/Migrations` (marked `generated_code`). Create with the `dotnet ef` command below; the Host is the startup project and `UsersDbContextFactory` is the design-time factory.
- Handler tests use real in-memory SQLite (`UsersFixture`), not the EF in-memory provider.

### Rules

- Modules reference only `Shared`, never each other. Verify with `get_project_graph`.
- The Host references modules and wires them up; modules never reference the Host. Modules contribute their views/view models to the shell through a registration contract defined in `Shared`.
- Each module has its own `DbContext`; never reference another module's DbContext or internal types.
- Cross-module communication: integration events defined in `Shared/Contracts/Events`, published through Wolverine.
- Shared holds contracts and cross-cutting primitives only, no business logic.
- Module handlers, view models, consumers, and DbContexts are `internal`; only the module registration extension is `public`.
- View models contain no UI types (no `Window`, `Control`); handlers contain no view-model types.
- Views live next to their view model in the feature folder; `ViewLocator` maps `*ViewModel` to `*View` by name.
- Everything in the Host is `internal` (CA1515); tests get access through `InternalsVisibleTo`.

## Avalonia Conventions

- Detect the Avalonia version in `Directory.Packages.props` before using APIs; 11.x and 12.x differ (clipboard, drag-drop, compiled-binding default).
- Touch UI only from the UI thread; marshal with `Dispatcher.UIThread`. Long-running work goes through async handlers with `CancellationToken`.
- Prefer `{Binding}` with `x:DataType` (compiled) over reflection bindings; no code-behind logic beyond view wiring.
- Use styles/control themes in `App.axaml` or resource dictionaries, not inline per-control styling.
- Use `Avalonia.Headless.XUnit` for view tests; keep most tests at the view model / handler level.

## Coding Standards

- C# 14: primary constructors, collection expressions, `field` keyword, records, pattern matching
- File-scoped namespaces; no regions; comments explain why, not what
- `var` for obvious types; PascalCase public, `_camelCase` private fields; `Async` suffix
- Namespace module types under `Dotnetstore.PersonalApp.Modules.<Module>`
- Analyzers are strict (`TreatWarningsAsErrors`): no underscores in test method names (CA1707), types `internal sealed` by default (CA1515, CA1852)

## Skills

`modern-csharp`, `vertical-slice`, `project-structure`, `ef-core`, `messaging`, `caching`, `dependency-injection`, `error-handling`, `testing`, `configuration`, `serilog`, `workflow-mastery`, `avalonia` (and its `avalonia-mvvm`, `avalonia-xaml`, `avalonia-data-binding`, `avalonia-testing` subskills)

## MCP Tools

`.mcp.json` registers `cwm-roslyn-navigator` (install once: `dotnet tool install -g CWM.RoslynNavigator`). Prefer it over grep:

- `find_symbol` / `get_public_api` before modifying a type
- `find_references` before changing a signature
- `get_project_graph` to verify module boundaries
- `get_diagnostics` after changes

## Commands

```bash
dotnet build
dotnet run --project src/Dotnetstore.PersonalApp.Host
dotnet test
dotnet format --verify-no-changes

# Windows installer (self-contained win-x64, Inno Setup 6; version from <Version> in Directory.Build.props)
pwsh installer/build-installer.ps1   # -> artifacts/installer/Dotnetstore.PersonalApp-Setup-<version>.exe

# EF migration for a module
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/Dotnetstore.PersonalApp.Modules.<Module> \
  --startup-project src/Dotnetstore.PersonalApp.Host \
  --context <Module>DbContext
```

## Workflow

- Plan first for non-trivial tasks (3+ steps or architecture decisions).
- Verify before done: `dotnet build` and `dotnet test`, then `get_diagnostics`.
- Fix bugs autonomously; if an approach goes sideways, stop and re-plan.
- Capture corrections in memory so mistakes don't recur.

## Anti-patterns

Do NOT:

- Add ASP.NET Core, minimal APIs, or HTTP endpoints; this is a desktop app
- Use `DateTime.Now` (use `TimeProvider`), `new HttpClient()` (use `IHttpClientFactory`), `async void` (except UI event handlers), `.Result`/`.Wait()`
- Block or touch UI controls off the UI thread
- Put logic in `.axaml.cs` code-behind that belongs in a view model
- Use reflection bindings where compiled bindings work
- Expose domain entities directly to views; map to view-model state
- Wrap EF Core in repository abstractions
- Catch bare `Exception`; use string interpolation in log messages
- Reference another module's DbContext/internals, share tables, or call another module's handler directly
- Create a single god DbContext
- Put business logic in Shared

## Startup & Splash Screen

- `App` shows `SplashWindow` first, builds the DI container on a background thread, then runs `StartupRunner`, which executes every registered `IStartupTask` (defined in `Shared/Startup`) in order and reports `StartupProgress` to `SplashViewModel`.
- To add startup work (migrations, cache warm-up, seeding), implement `IStartupTask` in the module with a user-facing `Description` and register it in `Add<Module>Module(...)` via `services.AddSingleton<IStartupTask, MyTask>()`. Each task becomes one step on the progress bar.
- Startup tasks must not touch UI controls; the splash is updated through `Progress<T>` on the UI thread.
- A failure shows the error on the splash, then shuts the app down with exit code 1.

## Startup Flow & Authentication

- After the splash, `MainWindowViewModel.InitializeAsync` picks the first screen: no users -> **first-user setup**; users exist -> **login**. Creating the first user leads to login (never straight into the app). A successful login shows `ShellViewModel` (navigation + module pages); **Sign out** returns to login.
- The Host drives this through `IAuthenticationPages` (in `Shared/Auth`), implemented by the Users module (`LoginViewModel`, `FirstUserViewModel`, `Login`/`HasUsers` handlers). The Host never references Users' view models.
- Login errors are deliberately generic ("Invalid username or password.") and unknown usernames still pay the hashing cost, so neither message nor timing reveals which usernames exist.
- The signed-in `AuthenticatedUser` lives on `ShellViewModel.User`. There is no authorization (roles/permissions) yet and no lockout after repeated failures.

## Design System (admin template)

- Light admin look: dark slate sidebar, white top bar, light grey page background, indigo/violet accent, rounded cards. Tokens (`AccentBrush`, `SurfaceBrush`, `TextPrimaryBrush`, `BorderSubtleBrush`, `SidebarBrush`, ...) live in `Host/App.axaml`; reusable styles live in `Host/Styles/AdminTheme.axaml`. Always use these tokens via `DynamicResource`; never hardcode colors in module views.
- Style classes: `Border.card`, `TextBlock.h1/h2/muted/caption/label/error/column`, `Button.primary/subtle/danger/link-danger`, `ListBox.nav` (sidebar), `ListBox.table` (table-like list), `Border.avatar`.
- Page layout: modules render only the page content (a `Margin="28"` grid with an `h1` title, a `muted` subtitle and cards). The Host shell supplies sidebar, breadcrumb bar and sign-out.
- `IModulePage.IconData` is 24x24 SVG path data (Material icons work) rendered with `PathIcon` in the sidebar.
- IModulePage.Group (optional ModulePageGroup) nests a page under a collapsible sidebar header; pages from different modules join the same group by Key. Address/email/phone type pages share SettingsGroup; TimeReports and WorkTimeTypes share a "Time reports" group.
- Auth pages use a split layout: brand gradient panel on the left, form on the right.

## Localization (en, sv, zh-CN)

- Supported languages: English (default, neutral resources), Swedish (`sv`), Simplified Chinese (`zh-CN`, resource files `*.zh-Hans.resx`). The list lives in `Shared/Localization/Localizer.cs`.
- Each project owns its strings: `Host/Resources/HostStrings*.resx`, `Modules/Users/.../Resources/UsersStrings*.resx`. A module registers its `ResourceManager` in `Add<Module>Module` via `Localizer.Instance.AddResources(...)`. Keys are `Area_Name` with underscores (e.g. `Users_Title`, `Field_FirstName`, `Error_password_too_short`).
- XAML: `xmlns:loc="using:Dotnetstore.PersonalApp.Shared.Localization"` then `Text="{loc:Tr Users_Title}"`. Never hardcode user-facing text in XAML or view models.
- Code: inject `ILocalizationService` (`Get`, `Format`, `Describe(Error)`). Handlers never return display text: they return `Error` with a stable `Code` (validation rules use `WithErrorCode`), and the view model shows `localization.Describe(error)` (resource `Error_{code}`, falling back to the English message).
- Switching language rebuilds the current screen (`MainWindowViewModel` reacts to `CultureChanged`), so strings are looked up once at view/view-model creation and need no change notification. Do not cache localized text in long-lived objects.
- The choice is saved in `%LOCALAPPDATA%/Dotnetstore.PersonalApp/settings.json`; with no saved choice the system language is used if supported, else English. `<loc:LanguageComboBox/>` is the drop-in picker (auth pages and shell top bar).
- Adding a language: add it to `Localizer.Languages`, add `*.<culture>.resx` next to every `*Strings.resx` (`GenResx`-style: same keys). Tests (`EveryEnglishKeyIsTranslated`) fail if any key is missing or extra in a language.
- `InvariantGlobalization` is **off** (real culture data is needed for dates and calendars); keep it off.
