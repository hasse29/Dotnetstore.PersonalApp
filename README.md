# Dotnetstore.PersonalApp

[![Build](https://github.com/hasse29/Dotnetstore.PersonalApp/actions/workflows/build.yml/badge.svg)](https://github.com/hasse29/Dotnetstore.PersonalApp/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/hasse29/Dotnetstore.PersonalApp)](https://github.com/hasse29/Dotnetstore.PersonalApp/releases/latest)
[![Last commit](https://img.shields.io/github/last-commit/hasse29/Dotnetstore.PersonalApp)](https://github.com/hasse29/Dotnetstore.PersonalApp/commits/main)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![Avalonia 12](https://img.shields.io/badge/Avalonia-12-8B44AC)
![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows)

A personal desktop app for keeping track of clients, the time you work for them, your CVs and your mail. Built with [Avalonia](https://avaloniaui.net/) on .NET 10 as a modular monolith.

## Features

- **Clients**: companies with a unique organization number, a photo, and any number of **contact persons** (each with photo, addresses, email addresses and phone numbers). Companies have addresses, email addresses and phone numbers too.
- **Students**: a client is a school. Each school has any number of students (photo, name, class, notes) with their own addresses, email addresses and phone numbers, and a running, dated log of notes (latest entry first). Students have their own page across all schools, with a school filter, and a tab on each school.
- **Subjects**: a catalogue of subjects. A teacher can teach several, picked on the user's page; several teachers can share a subject.
- **Grades**: grade scales per country (for example A-F in Sweden or 1-6 in Germany, defined by you) and the grades you give your students per subject, on a Grades page and a tab on each student. Tests record a name, date and maximum score, then each student's result and grade.
- **Schedule**: a teacher's recurring weekly lessons, for example mathematics every Monday at 08:00, each with a subject, a school, a time, an optional room and the students who attend. Overlapping lessons are rejected.
- **Time reports**: date, start and end time, worked hours, work time type, location and driving distance per client.
  - Copy a report to create similar ones on another date.
  - **Work time types** (Work, Sick, Vacation, Parental leave, or your own) have their own page.
  - **Excel export** of one client's reports for a year, one sheet per month.
  - **Kilometer summary** for a year, per client and month with totals, for tax reporting.
  - **Email the export** to the client or a contact person.
- **CVs**: several CVs per user for different jobs, with photo, summary, work experience (with technologies), education, courses, languages, driver licence, LinkedIn link and the contact details you choose per CV. Export to Word.
- **Email**: SMTP and POP3 accounts (personal or shared system accounts), inbox with auto-refresh, reply, reply all, forward, drafts, sent mail, attachments and formatted HTML mail.
- **Users and sign-in**: the first start creates the first user; users have a photo that shows in the sidebar and top bar.
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

One process, strict module boundaries. Each module owns its views, view models, handlers, data and domain logic as vertical slices. Modules reference only `Shared`, never each other, and talk through contracts and integration events defined there.

| Module | Purpose | Database |
| --- | --- | --- |
| Clients | Client companies and contact persons | `clients.db` |
| Students | Students of each school | `students.db` |
| Subjects | Subjects and which teacher teaches what | `subjects.db` |
| Schedule | A teacher's recurring weekly lessons | `schedule.db` |
| Grades | Grade scales per country and students' grades | `grades.db` |
| Users | Users and sign-in | `users.db` |
| Addresses | Addresses for any object, with address types | `addresses.db` |
| EmailAddresses | Email addresses for any object, with types | `emailaddresses.db` |
| PhoneNumbers | Phone numbers for any object, with types | `phonenumbers.db` |
| TimeReports | Time reports, work time types, exports | `timereports.db` |
| Cvs | CVs with sections, contact selection, Word export | `cvs.db` |
| Email | Mail accounts, inbox, sending, drafts | `email.db` |

```
src/
  Dotnetstore.PersonalApp.Host/      Avalonia app, shell, composition root
  Dotnetstore.PersonalApp.Shared/    Contracts, events, Result, localization
  Modules/<Module>/...               One project per module
tests/                               xUnit v3 tests (Shouldly) per module plus integration tests
installer/                           Inno Setup script and build script
docs/                                Architecture and module documentation
```

**Tech stack:** .NET 10, Avalonia 12 (Fluent theme, compiled bindings), CommunityToolkit.Mvvm, EF Core with SQLite, FluentValidation, SkiaSharp, MailKit, ClosedXML, Open XML SDK, xUnit v3 with Shouldly.

## Documentation

- [docs/architecture.md](docs/architecture.md): boundaries, conventions, events, startup, localization, design system, testing.
- [docs/modules.md](docs/modules.md): what each module does and its rules.
- [CLAUDE.md](CLAUDE.md): instructions for AI-assisted development.

## Known limitations

- Windows only (the installer, and Email's password protection through DPAPI).
- Integration events use a small in-process dispatcher without an outbox, so a crash between a delete and its follow-up can leave orphaned rows.
- Time reports cannot span midnight. There are no roles, and no lockout after repeated failed sign-ins.
- Photos saved before thumbnails existed show initials in lists until uploaded again.
- Swedish and Chinese translations are machine-assisted and have not had a native review.
