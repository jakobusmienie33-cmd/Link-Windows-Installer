# The Link — Windows Installer

Source repository for **The Link Windows Installation & Configuration Wizard**.

> Current implementation: **LWI-P0/14 through LWI-P7/14**.

## Canonical UI reference

The implementation follows `the_link_windows_installer_comprehensive_mockup.html` as the canonical Windows installer UI and interaction reference. The production source preserves its 12-step wizard, dark Link header/sidebar, green status language, guided cards, workstation profiles, database choices and install-path flow.

**Branding attribution:** By MeetWell Technologies.

## Completed phases

- **LWI-P0/14 — Repository & Architecture Foundation**
- **LWI-P1/14 — Installer Shell & Branding**
- **LWI-P2/14 — Windows Environment Detection**
- **LWI-P3/14 — Installation Location & File Layout**
- **LWI-P4/14 — Prerequisite Manager**
- **LWI-P5/14 — PostgreSQL Detection**
- **LWI-P6/14 — Database Setup**
- **LWI-P7/14 — Database Bootstrap & Security**

The final installer executable is **not generated automatically**. Source validation and build checks are provided; final EXE packaging remains a manual release step.

## Implemented setup flow

The working source now covers:

```text
Welcome
  -> System Check
  -> Install Type
  -> Components / Prerequisites
  -> Database / SQL
  -> Install Location
```

Later screens remain visible in the 12-step shell and are implemented from P8 onward.

## Technology

- C# / .NET 8
- WPF
- No third-party UI framework
- Windows-native system and registry probing
- Platform-neutral installer policy rules in `Link.Windows.Installer.Core`
- Windows Credential Manager for validated PostgreSQL runtime secrets
- Architecture prepared for install, repair, upgrade and uninstall engines

## Development

On Windows with the .NET 8 SDK:

```powershell
dotnet restore
dotnet build .\Link.Windows.Installer.sln -c Debug
dotnet run --project .\tests\Link.Windows.Installer.Core.SmokeTests\Link.Windows.Installer.Core.SmokeTests.csproj -c Debug
```

Run source validation:

```powershell
.\scripts\validate-source.ps1 -Build
```

## Database deployment rule

A normal workstation uses **Cloud Supabase/PostgreSQL + local SQLite**. Local PostgreSQL is an advanced branch/server option only.

The desktop installer must never store a production Supabase `service_role` secret or run production cloud DDL from an ordinary workstation installation.

For approved local/branch PostgreSQL, the installer uses a separate privileged migration/bootstrap identity and a restricted runtime identity. Runtime secrets are stored through Windows Credential Manager rather than plaintext configuration.

See:
- `docs/DATABASE_SECURITY.md`
- `docs/INSTALLATION_LAYOUT.md`
- `docs/PREREQUISITES.md`
- `docs/PHASES.md`

## Release rule

P8 will consume a prepared Link-Core Windows release artifact. Link-Core source code is not copied into this repository.

**By MeetWell Technologies**
