# Link Windows Installer Architecture

## Purpose

`Link-Windows-Installer` owns Windows installation orchestration. It does **not** own Link-Core business logic, production Supabase schema administration or organisation permissions.

## Projects

### Link.Windows.Installer.Core
Platform-neutral rules and models:
- compatibility evaluation,
- installation layout policy,
- prerequisite state/evaluation,
- database-mode planning,
- database bootstrap/security invariants.

### Link.Windows.Installer
Windows/WPF implementation:
- canonical installer UI,
- Windows system probing,
- prerequisite registry probing,
- PostgreSQL discovery,
- endpoint/psql validation,
- Windows Credential Manager,
- setup logging,
- later file/service/update/uninstall execution.

## Current flow

```text
Welcome
  -> System Check
  -> Install Type
  -> Components + Prerequisites
  -> Database / SQL
  -> Install Location
  -> P8+ execution/configuration screens
```

The screen order follows the canonical HTML even though implementation phases are organised by engineering dependency.

## Data architecture rules

### Normal workstation
```text
The Link Windows
  -> local SQLite cache/offline layer
  -> authenticated Link/Supabase backend
```

The installer verifies client-safe backend configuration and reachability. It never stores production `service_role` credentials and never runs production cloud DDL.

### Approved branch/server
Existing or locally installed PostgreSQL may be used only for an explicitly approved deployment profile. Bootstrap is driven by a signed migration bundle and separates privileged migration identity from restricted runtime identity.

## Filesystem baseline

Per-machine default:
- Application: `C:\Program Files\The Link`
- Mutable data: `C:\ProgramData\The Link`
- SQLite/cache: `C:\ProgramData\The Link\Data`
- Setup logs: `C:\ProgramData\The Link\Logs\Setup`
- Migration backups: `C:\ProgramData\The Link\Backups`

Application binaries and mutable business data are never intentionally co-located.

## Release boundary

The installer consumes a prepared Link-Core Windows release artifact in P8. Final EXE packaging remains a manual release operation.
