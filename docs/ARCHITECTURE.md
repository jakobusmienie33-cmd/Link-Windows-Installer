# Link Windows Installer Architecture

## Boundaries

`Link-Windows-Installer` owns Windows setup orchestration, not Link-Core business logic and not production Supabase administration.

### Core project
Contains platform-neutral compatibility, path, prerequisite, database, release-manifest, deployment, upgrade/repair and uninstall policy.

### Windows project
Contains the WPF wizard, Windows probing, Credential Manager, release payload verification, transactional file deployment, runtime config/state persistence, shortcuts, startup/uninstall metadata, optional service registration and uninstall execution.

## Deployment

```text
prepared Link-Core Windows release
  -> release-manifest.json + SHA-256/size verification
  -> Review
  -> application checkpoint
  -> transactional deployment
  -> client-safe runtime configuration
  -> Windows integration
  -> persisted installation state
  -> post-install validation
```

Install state determines Install, Upgrade or Repair. Standard downgrades are blocked.

## Data safety

Application binaries live separately from mutable business/cache data. Normal uninstall preserves data/logs/backups. `--purge-data` is explicit and destructive.

## Database safety

Normal workstation mode uses cloud backend + local SQLite. Production cloud DDL and production `service_role` storage are prohibited.

## Packaging

Source version is 1.0.0-rc1. Final EXE creation/signing remains manual.


## Automatic update extension

Link-Core remains the Release Control authority. This repository supplies the Windows-native bootstrap/update adapter.

The target layout separates stable bootstrap binaries from versioned Flutter application releases:

```text
Program Files/The Link/Launcher
Program Files/The Link/Updater
Program Files/The Link/Versions/<version>
ProgramData/The Link/Updater/{Staging,current-version.txt,previous-version.txt}
```

The updater downloads and verifies in the background, stages without touching the running version, and activates only at a Link-Core-declared safe point. Automatic is the default policy; Scheduled and Manual remain available. The previous known-good version is retained for rollback.

See `docs/AUTOMATIC_UPDATES.md`.
