# Dotnetstore.PersonalApp — Avalonia Modular Monolith

.NET 11 **Avalonia desktop app** (MVVM) as a modular monolith: one process (the Host), strict module boundaries, vertical slices inside each module. Solo project, CRUD-heavy. No HTTP API, no minimal API.

## Documentation

- [docs/architecture.md](docs/architecture.md): layout, boundaries, module conventions, integration events, startup/auth, localization, design system, testing. Read before structural work.
- [docs/modules.md](docs/modules.md): what each module owns and its rules (Clients, Students, Subjects, Schedule, Grades and tests, Users, Contact (addresses, email addresses, phone numbers), TimeReports, Cvs, Email, photos). Read before changing a module.
- [README.md](README.md): user-facing overview, install and build.

Keep these in sync when you change a module's behavior, add a module, or change a convention. Do not copy their content here.

## Tech Stack

- **.NET 11** / C# 14 (`Directory.Build.props`: nullable, `AnalysisLevel=latest-all`, warnings as errors)
- **Avalonia 12.1.x**, Fluent theme, compiled bindings on by default (`x:DataType` required in every `.axaml`)
- **CommunityToolkit.Mvvm** (`[ObservableProperty]`, `[RelayCommand]`), **Microsoft.Extensions.DependencyInjection** (composition root in `Host/CompositionRoot.cs`)
- **EF Core + SQLite**, one DbContext and one database file per module
- **FluentValidation**; Wolverine and Serilog are not used (see integration events in the architecture doc); no auth framework
- **xUnit v3** with **Shouldly** assertions (never `Assert.*`) and `Avalonia.Headless.XUnit` for UI tests
- Central package management: add versions to `Directory.Packages.props`, reference without a version in the csproj

## Rules

- Modules reference only `Shared`, never each other (verify with `get_project_graph`). The Host wires modules up; modules never reference the Host.
- Each module has its own DbContext; never reference another module's DbContext or internals, share tables, or call another module's handler. Cross-module communication is contracts and integration events in `Shared`.
- `Shared` holds contracts and primitives only, no business logic.
- Handlers, view models, consumers and DbContexts are `internal`; only `Add<Module>Module` is `public`. Everything in the Host is `internal` (CA1515).
- View models contain no UI types; handlers contain no view-model types; no logic in `.axaml.cs`.
- Handlers return `Error` codes (prefixed per module), never display text. All user-facing text comes from `.resx` in en, sv and zh-Hans.
- Error codes, resource keys and handler patterns: see [docs/architecture.md](docs/architecture.md).

## Avalonia Conventions

- Check the Avalonia version in `Directory.Packages.props` before using APIs; 11.x and 12.x differ (clipboard, drag-drop, compiled bindings).
- Touch UI only from the UI thread (`Dispatcher.UIThread`); long-running work goes through async handlers with `CancellationToken`.
- Prefer compiled `{Binding}` with `x:DataType` over reflection bindings.
- Use styles and tokens from `App.axaml` / `AdminTheme.axaml` via `DynamicResource`, not inline per-control styling or hardcoded colors.
- Keep most tests at view-model or handler level.

## Coding Standards

- C# 14: primary constructors, collection expressions, `field` keyword, records, pattern matching
- File-scoped namespaces; no regions; comments explain why, not what
- `var` for obvious types; PascalCase public, `_camelCase` private fields; `Async` suffix
- Namespace module types under `Dotnetstore.PersonalApp.Modules.<Module>`
- Strict analyzers (`TreatWarningsAsErrors`): types `internal sealed` by default (CA1515, CA1852); no underscores in test method names (CA1707)

## Anti-patterns

Do NOT:

- Add ASP.NET Core, minimal APIs or HTTP endpoints
- Use `DateTime.Now` (use `TimeProvider`), `new HttpClient()` (use `IHttpClientFactory`), `async void` (except UI event handlers), `.Result` / `.Wait()`
- Block or touch UI controls off the UI thread
- Put logic in `.axaml.cs` that belongs in a view model
- Use reflection bindings where compiled bindings work
- Expose domain entities to views; map to view-model state
- Wrap EF Core in repository abstractions
- Catch bare `Exception`; use string interpolation in log messages
- Create a single god DbContext
- Put business logic in Shared
- Store or log plain-text passwords

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
- Add a module with `/scaffold`.

## Skills and MCP

Skills: `modern-csharp`, `vertical-slice`, `project-structure`, `ef-core`, `messaging`, `caching`, `dependency-injection`, `error-handling`, `testing`, `configuration`, `serilog`, `workflow-mastery`, `avalonia` (and `avalonia-mvvm`, `avalonia-xaml`, `avalonia-data-binding`, `avalonia-testing`).

`.mcp.json` registers `cwm-roslyn-navigator` (install once: `dotnet tool install -g CWM.RoslynNavigator`). Prefer it over grep: `find_symbol` / `get_public_api` before modifying a type, `find_references` before changing a signature, `get_project_graph` for module boundaries, `get_diagnostics` after changes.
