# The Link — Windows Installer

Source repository for **The Link Windows Installation & Configuration Wizard**.

> Current implementation: **LWI-P0/14 through LWI-P2/14**.

## Canonical UI reference

The implementation follows `the_link_windows_installer_comprehensive_mockup.html` as the canonical Windows installer UI and interaction reference. The production source preserves its 12-step wizard, dark Link header/sidebar, green status language, card layouts, workstation profiles, database deployment rules and guided setup model.

**Branding attribution:** By MeetWell Technologies.

## Completed foundation

- **LWI-P0/14 — Repository & Architecture Foundation**
- **LWI-P1/14 — Installer Shell & Branding**
- **LWI-P2/14 — Windows Environment Detection**

The final installer executable is **not generated automatically**. Source validation and build checks are provided; final EXE packaging remains a manual release step.

## Technology

- C# / .NET 8
- WPF
- No third-party UI framework
- Windows-native system probing
- Platform-neutral compatibility rules separated from Windows probing
- Architecture prepared for install, repair, upgrade and uninstall engines

## Development

On Windows with the .NET 8 SDK:

```powershell
dotnet restore
dotnet build .\Link.Windows.Installer.sln -c Debug
```

Run source validation:

```powershell
.\scripts\validate-source.ps1
```

## Database deployment rule

A normal workstation uses **Cloud Supabase/PostgreSQL + local SQLite**. Local PostgreSQL is an advanced branch/server option only.

The desktop installer must never store a production Supabase `service_role` secret or run production cloud DDL from an ordinary workstation installation.

## Release rule

The installer will consume a prepared Link-Core Windows release artifact in later phases. Link-Core source code is not copied into this repository.

**By MeetWell Technologies**
