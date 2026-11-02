# Architecture

A .NET 10 Avalonia desktop app (MVVM) structured as a modular monolith. Modules live in one process (the Host) but keep strict boundaries: each owns its features (views, view models, handlers), data and domain logic. The domain is CRUD-heavy and the team is solo, so each module uses vertical slices internally. There is no HTTP API.

Module details are in [modules.md](modules.md).

## Solution layout

```
src/
  Dotnetstore.PersonalApp.Host/      Avalonia app: App.axaml, Program.cs, MainWindow, shell, CompositionRoot
  Dotnetstore.PersonalApp.Shared/    Thin shared kernel: contracts, events, Result, localization, primitives
  Modules/<Module>/Dotnetstore.PersonalApp.Modules.<Module>/
    Features/<Feature>/
      <Feature>View.axaml(.cs)       Avalonia view
      <Feature>ViewModel.cs          View model
      <Operation>.cs                 Command/Query + Validator + Handler
    Persistence/<Module>DbContext.cs
    Persistence/Migrations/          generated_code
    Consumers/                       Handlers for integration events
    Resources/<Module>Strings*.resx
    <Module>Module.cs                public Add<Module>Module(...)
tests/
  Dotnetstore.PersonalApp.Integration.Tests/    Composition root and cross-module tests
  Modules/Dotnetstore.PersonalApp.Modules.<Module>.Tests/
installer/                           Inno Setup script and build script
```

## Boundaries

- Modules reference only `Shared`, never each other. The Host references modules and wires them up; modules never reference the Host.
- Modules contribute pages to the shell through `IModulePage` (defined in `Shared`).
- Each module has its own `DbContext` and database; never share tables or reference another module's DbContext or internals.
- Cross-module data and UI goes through contracts in `Shared`: lookups (`IClientLookup`, `IUserLookup`, ...), panel factories (`IObjectAddressesPanelFactory`, ...), `IContactInfoSource`, `ISystemMailSender`, and integration events.
- `Shared` holds contracts and cross-cutting primitives only, no business logic.
- Handlers, view models, consumers and DbContexts are `internal`; only the module registration extension is `public`. Everything in the Host is `internal` (tests use `InternalsVisibleTo`).
- View models contain no UI types (`Window`, `Control`); handlers contain no view-model types. Views live next to their view model, and `ViewLocator` maps `*ViewModel` to `*View` by name.

## Module conventions

- Handlers are `internal static class <Op>` wrappers holding `Command`/`Query`, an optional `Validator`, and a `Handler` that takes `IDbContextFactory<TDbContext>` (a context per operation, since view models are long-lived) and returns `Result<T>` (`Shared.Results`).
- Handlers never return display text: they return an `Error` with a stable `Code` (validation rules use `WithErrorCode`). Error codes are prefixed per module (`user_*`, `client_*`, ...).
- A module exposes `Add<Module>Module(this IServiceCollection, ...)`, registers an `IModulePage` for navigation, an `IStartupTask` that applies its EF migrations, and its resource strings.
- `IModulePage.IconData` is 24x24 SVG path data (Material icons work). `IModulePage.Group` (optional `ModulePageGroup`) nests a page under a collapsible sidebar header; pages from different modules join the same group by key. Address/email/phone type pages share the Settings group; TimeReports and WorkTimeTypes share "Time reports".
- Migrations live in `Persistence/Migrations`. The Host is the startup project and `<Module>DbContextFactory` is the design-time factory.

## Integration events

Wolverine needs a generic host this app does not have, so events use a minimal in-process dispatcher: contracts `IEventPublisher` / `IIntegrationEventConsumer<TEvent>` in `Shared/Events`, event records in `Shared/Contracts/Events`, implementation `InProcessEventPublisher` in the Host. A module publishes after its own change succeeds; consumers live in `Consumers/` and are registered as `IIntegrationEventConsumer<T>`.

Delivery is synchronous with no outbox, so a crash between a delete and its consumer leaves orphans.

| Event | Published by | Consumed by |
| --- | --- | --- |
| `UserDeleted` | Users | Addresses, EmailAddresses, PhoneNumbers, Cvs, Email, Subjects |
| `ClientDeleted` | Clients | Addresses, EmailAddresses, PhoneNumbers, Students |
| `ContactPersonDeleted` | Clients | Addresses, EmailAddresses, PhoneNumbers |
| `StudentDeleted` | Students | Addresses, EmailAddresses, PhoneNumbers |
| `UserPhotoChanged` | Users | Host (`UserPhotoNotifier`) |

## Startup and authentication

- `App` shows `SplashWindow`, builds the DI container on a background thread, then runs `StartupRunner`, which executes every registered `IStartupTask` (`Shared/Startup`) in order and reports `StartupProgress` to `SplashViewModel`. Each task is one step on the progress bar and must not touch UI controls. A failure shows the error on the splash and exits with code 1.
- To add startup work (migrations, seeding, warm-up), implement `IStartupTask` with a user-facing `Description` and register it in `Add<Module>Module(...)`.
- `MainWindowViewModel.InitializeAsync` then picks the first screen: no users leads to first-user setup, otherwise login. Creating the first user leads to login, never straight into the app. A successful login shows `ShellViewModel` (navigation and module pages); Sign out returns to login.
- The Host drives this through `IAuthenticationPages` (`Shared/Auth`), implemented by Users, so the Host never references Users' view models.
- Login errors are deliberately generic and unknown usernames still pay the hashing cost, so neither message nor timing reveals which usernames exist.
- The signed-in `AuthenticatedUser` lives on `ShellViewModel.User`; `ICurrentUser` exposes it to modules. There is no authorization (roles) and no lockout after repeated failures.

## Localization (en, sv, zh-CN)

- Languages: English (default, neutral resources), Swedish (`sv`) and Simplified Chinese (`zh-CN`, files `*.zh-Hans.resx`). The list lives in `Shared/Localization/Localizer.cs`.
- Each project owns its strings (`Host/Resources/HostStrings*.resx`, `<Module>/Resources/<Module>Strings*.resx`) and a module registers its `ResourceManager` via `Localizer.Instance.AddResources(...)`. Strings used by several modules (e.g. `Common_Cancel`) live in `Shared/Resources/SharedStrings*.resx`. Keys are `Area_Name` (`Users_Title`, `Field_FirstName`, `Error_password_too_short`).
- XAML: `xmlns:loc="using:Dotnetstore.PersonalApp.Shared.Localization"` then `Text="{loc:Tr Users_Title}"`. Code: inject `ILocalizationService` (`Get`, `Format`, `Describe(Error)`); view models show `localization.Describe(error)` (resource `Error_{code}`, falling back to the English message). Never hardcode user-facing text.
- Switching language rebuilds the current screen (`MainWindowViewModel` reacts to `CultureChanged`), so strings are looked up once at view or view-model creation. Do not cache localized text in long-lived objects.
- The choice is saved in `%LOCALAPPDATA%/Dotnetstore.PersonalApp/settings.json`; with none saved the system language is used if supported, else English. `<loc:LanguageComboBox/>` is the drop-in picker.
- To add a language: add it to `Localizer.Languages` and a `*.<culture>.resx` next to every `*Strings.resx` with the same keys. The `EveryEnglishKeyIsTranslated` test fails on missing or extra keys.
- `InvariantGlobalization` is off (real culture data is needed for dates and calendars); keep it off.

## Design system

Light admin look: dark slate sidebar, white top bar, light grey page background, indigo/violet accent, rounded cards.

- Tokens (`AccentBrush`, `SurfaceBrush`, `TextPrimaryBrush`, `BorderSubtleBrush`, `SidebarBrush`, ...) live in `Host/App.axaml`; reusable styles in `Host/Styles/AdminTheme.axaml`. Use tokens via `DynamicResource`; never hardcode colors in module views.
- Style classes: `Border.card`, `TextBlock.h1/h2/muted/caption/label/error/column`, `Button.primary/subtle/danger/link-danger`, `ListBox.nav`, `ListBox.table`, `Border.avatar`.
- Modules render only page content: a `Margin="28"` grid with an `h1` title, a `muted` subtitle and cards. The Host shell supplies sidebar, breadcrumb bar and sign-out.
- Auth pages use a split layout: brand gradient panel on the left, form on the right.

## Testing

xUnit v3 with Shouldly assertions (never `Assert.*`), and `Avalonia.Headless.XUnit` for the few view tests. Most tests are at handler or view-model level; handler tests use real in-memory SQLite (e.g. `UsersFixture`), not the EF in-memory provider. Module `Add<Module>Module` registration and the composition root are covered by the integration tests. Analyzers are strict: no underscores in test method names (CA1707).
