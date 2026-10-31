# Dotnetstore.PersonalApp

[![Build](https://github.com/hasse29/Dotnetstore.PersonalApp/actions/workflows/build.yml/badge.svg)](https://github.com/hasse29/Dotnetstore.PersonalApp/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/hasse29/Dotnetstore.PersonalApp)](https://github.com/hasse29/Dotnetstore.PersonalApp/releases/latest)
[![Last commit](https://img.shields.io/github/last-commit/hasse29/Dotnetstore.PersonalApp)](https://github.com/hasse29/Dotnetstore.PersonalApp/commits/main)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![Avalonia 12](https://img.shields.io/badge/Avalonia-12-8B44AC)
![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows)

A personal desktop app for keeping track of clients and the time you work for them. Built with [Avalonia](https://avaloniaui.net/) on .NET 10 as a modular monolith.

## Features

- **Clients**: companies with a unique organization number, plus their addresses, email addresses and phone numbers.
- **Time reports**: report date, start and end time, worked hours, work time type, location and driving distance per client.
  - Copy a report to quickly create similar ones on another date.
  - **Work time types** (Work, Sick, Vacation, Parental leave, or your own) are managed on their own page.
  - **Excel export** of one client's reports for a year, one sheet per month (`2026-01`, `2026-02`, ...) with date, hours and location.
  - **Kilometer summary** for a year, per client and month with totals, handy for tax reporting.
- **Users and sign-in**: the first start creates the first user; every report belongs to the signed-in user.
- **Languages**: English, Swedish and Simplified Chinese, switchable at any time.

## Install

Download `Dotnetstore.PersonalApp-Setup-<version>.exe` from the [latest release](https://github.com/hasse29/Dotnetstore.PersonalApp/releases/latest) and run it. The app is self-contained, so .NET does not need to be installed. The installer is not code-signed, so Windows SmartScreen may warn about an unknown publisher.

Your data is stored in `%LOCALAPPDATA%\Dotnetstore.PersonalApp` (one SQLite database per module plus `settings.json`) and is kept when you uninstall.

## Build and run from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet run --project src/Dotnetstore.PersonalApp.Host
dotnet test
```

### Build the installer

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`).

```powershell
pwsh installer/build-installer.ps1
```

The result is `artifacts/installer/Dotnetstore.PersonalApp-Setup-<version>.exe`. The version comes from `<Version>` in `Directory.Build.props`.

## Architecture

One process, strict module boundaries. Each module owns its views, view models, handlers, data and domain logic, organized as vertical slices. Modules reference only `Shared`, never each other, and talk through contracts and integration events defined there.

| Module | Purpose | Database |
| --- | --- | --- |
| Users | Users and sign-in | `users.db` |
| Clients | Client companies | `clients.db` |
| Addresses | Addresses for any object, with address types | `addresses.db` |
| EmailAddresses | Email addresses for any object, with types | `emailaddresses.db` |
| PhoneNumbers | Phone numbers for any object, with types | `phonenumbers.db` |
| TimeReports | Time reports, work time types and exports | `timereports.db` |

```
src/
  Dotnetstore.PersonalApp.Host/      Avalonia app, shell, composition root
  Dotnetstore.PersonalApp.Shared/    Contracts, Result, localization, events
  Modules/<Module>/...               One project per module
tests/                               xUnit v3 tests per module plus integration tests
installer/                           Inno Setup script and build script
```

**Tech stack:** .NET 10, Avalonia 12 (Fluent theme, compiled bindings), CommunityToolkit.Mvvm, EF Core with SQLite, FluentValidation, ClosedXML for Excel, xUnit v3.

More detail on conventions, module design and decisions is in [CLAUDE.md](CLAUDE.md).

## Known limitations

- Windows only for now (the installer and testing target Windows).
- Integration events use a small in-process dispatcher without an outbox, so a crash between a delete and its follow-up can leave orphaned rows.
- Time reports cannot span midnight, and there is no lockout after repeated failed sign-ins.
- Swedish and Chinese translations are machine-assisted and have not had a native review.