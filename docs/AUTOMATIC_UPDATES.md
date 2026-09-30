# Automatic Windows Updates

Status: **LWI-P14/18 source foundation implemented; validation pending**

The Link Windows update path is part of the Link-Core Release Control system. No new top-level updater repository is required.

## Ownership

- **Link-Core** decides which complete release is allowed for a device/organisation, including Core + module versions, channel, compatibility, rollout status and rollback directives.
- **Link-Windows-Installer** owns the privileged Windows bootstrap, launcher/updater adapter, version-slot file operations and Windows service integration.
- A normal user sees one product and one update mechanism.

## Stable Windows layout target

```text
C:\Program Files\The Link\
  Launcher\
  Updater\
  Versions\
    1.8.4\
    1.9.0\

C:\ProgramData\The Link\
  Data\
  Config\
  Logs\
  Backups\
  Updater\
    Staging\
    current-version.txt
    previous-version.txt
```

The launcher and updater are stable bootstrap components. Versioned application folders are replaceable. Mutable business/offline data never lives inside a version folder.

## Automatic update lifecycle

```text
check -> resolve -> download -> verify -> stage -> wait for safe point
      -> activate -> health check -> retain previous -> later cleanup
```

Default policy is **Automatic**.

Scheduled policy downloads/stages automatically but activates only in the configured maintenance window.

Manual policy does not activate automatically and requires authorised approval.

## Safe activation rule

The updater must never kill the running application during a protected workflow. Link-Core and modules will expose safe-state blockers for payments, cash-up, payroll, posting, stock/traceability transfers, migrations and other protected transactions.

Downloading and verification may continue in the background while activation is deferred.

## Rollback

A locally retained previous version provides the immediate rollback path. Release Control may issue a rollback directive. The updater switches the current pointer back only when safe, then health-checks the restored version.

Rollback-compatible database changes are mandatory; destructive schema contraction happens only after the rollback window expires.

## Security requirements

Production implementation must include:
- signed application/package artifacts;
- signed release manifest;
- SHA-256 file/package checks;
- TLS download;
- safe path validation;
- updater minimum-version gate;
- fail-closed signature/compatibility handling;
- audit logs for download/stage/activation/rollback;
- release withdrawal kill switch.

## Phase extension

The original installer programme completed P0-P13 at source level. It is extended to 18 total phases:

### LWI-P14/18 — Automatic updater architecture & planning
**Status: SOURCE FOUNDATION IMPLEMENTED / validation pending**

Version-slot layout contract, update policy/action contracts and safe activation/rollback planner.

### LWI-P15/18 — Stable launcher & version slots
Implement stable launcher/updater directories, current/previous pointers, version-specific installation, shortcut target migration and bootstrap self-preservation.

### LWI-P16/18 — Background download, verification & activation
Implement Release Control polling, resumable download, signature/hash verification, staging, safe-state handshake, scheduled/manual/automatic policy and atomic activation.

### LWI-P17/18 — Health, rollback, cleanup & acceptance
Implement startup health confirmation, failed-start rollback, remote rollback/withdrawal, retained-version cleanup, interrupted-update recovery, service hardening and end-to-end manual validation.

## Current limitation

The existing installer still deploys directly into the application directory. LWI-P15/18 is the controlled migration from direct replacement to stable bootstrap + version slots. Until then, existing transactional checkpoint rollback remains the installer safety mechanism.
