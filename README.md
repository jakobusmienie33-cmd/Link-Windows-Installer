# The Link — Windows Installer

Source repository for **The Link Windows Installation & Configuration Wizard**.

> Status: LWI-P0/14 through LWI-P2/14 foundation.

## Canonical UI reference

The implementation follows `the_link_windows_installer_comprehensive_mockup.html` as the canonical Windows installer UI and interaction reference. The production source preserves the mockup's 12-step wizard, The Link visual language, system-check experience, workstation profiles and database deployment rules.

## Current phase scope

- **LWI-P0/14 — Repository & Architecture Foundation**
- **LWI-P1/14 — Installer Shell & Branding**
- **LWI-P2/14 — Windows Environment Detection**

The final installer executable is **not generated in this repository automatically**. Source validation and build preparation are provided; final EXE packaging remains a manual release step.

## Technology

- C# / .NET 8
- WPF Windows desktop UI
- No third-party UI framework
- Windows-native compatibility checks
- Architecture prepared for later install, repair, upgrade and uninstall engines

## Development

Open `Link.Windows.Installer.sln` in Visual Studio 2022 or build from a Windows machine with the .NET 8 SDK:

```powershell
dotnet restore
dotnet build .\Link.Windows.Installer.sln -c Debug
```

Source-only validation:

```powershell
.\scripts\validate-source.ps1
```

## Release policy

The installer consumes prepared Link-Core Windows release artifacts. It must not contain production Supabase `service_role` secrets and normal workstation setup must not execute production DDL migrations.

Built@Home
