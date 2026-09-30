# The Link — Windows Installer

Production-candidate source for **The Link Windows Installation & Configuration Wizard**.

> Phase status: **LWI-P0/18 through LWI-P13/18 source-complete; LWI-P14/18 updater foundation implemented / validation pending; LWI-P15/18–LWI-P17/18 planned**  
> Source version: **1.0.0-rc1**  
> Final EXE packaging: **manual**

## Canonical UI

The installer follows `the_link_windows_installer_comprehensive_mockup.html` as the canonical UI reference. The WPF implementation includes all 12 screens:

```text
Welcome
System Check
Install Type
Components
Database / SQL
Install Location
Device & Services
Network & Security
Updates & Privacy
Review
Installing
Complete
```

Branding attribution: **By MeetWell Technologies**.

## What is implemented

- Windows 10/11 x64/ARM64 compatibility probing
- prerequisite detection
- Program Files / ProgramData layout
- cloud + local SQLite default architecture
- advanced PostgreSQL branch/server setup paths
- no production cloud DDL / no service_role storage
- Link-Core release manifest validation
- SHA-256 + size verification for every required staged file
- install / upgrade / repair mode detection
- transactional application rollback checkpoint
- automatic background update planning with Automatic/Scheduled/Manual policy
- stable launcher/updater + version-slot target architecture
- protected-workflow safe activation and retained-version rollback planning
- runtime configuration and installation-state persistence
- Windows Credential Manager for PostgreSQL runtime secrets
- Start Menu/Desktop/startup integration
- Add/Remove Programs metadata
- signed-payload service registration hooks
- uninstall preserving business/cache data by default
- explicit purge-data uninstall path
- manual packaging/readiness scripts

## Build

```powershell
dotnet restore .\Link.Windows.Installer.sln
dotnet build .\Link.Windows.Installer.sln -c Debug
dotnet run --project .\tests\Link.Windows.Installer.Core.SmokeTests\Link.Windows.Installer.Core.SmokeTests.csproj -c Debug
```

## Stage Link-Core

```powershell
.\scripts\new-release-manifest.ps1 -ReleaseFolder "<Link-Core Release>" -Version "1.0.0" -Architecture x64 -EntryPoint "the_link.exe"
.\scripts\stage-link-core.ps1 -Source "<Link-Core Release>"
.\scripts\verify-release.ps1
```

## Manual packaging gate

```powershell
.\scripts\manual-package-check.ps1
```

Then create/sign the final EXE using the manually controlled Windows packaging workflow.

See `docs/RELEASE_MANIFEST.md`, `docs/UPGRADE_REPAIR_UNINSTALL.md`, `docs/AUTOMATIC_UPDATES.md`, and `docs/MANUAL_PACKAGING.md`.

**By MeetWell Technologies**
