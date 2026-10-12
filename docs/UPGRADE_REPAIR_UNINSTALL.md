# Upgrade, Repair, Rollback & Uninstall

## Upgrade

When the installed version is older than the staged verified release, setup creates an application checkpoint, deploys the incoming payload, refreshes client-safe configuration and Windows integration metadata, persists the new state, and runs post-install validation.

If deployment fails after the checkpoint is created, the previous application directory is restored.

## Repair

When the staged version equals the installed version, setup enters repair mode and re-applies required files from the verified payload. Mutable business data remains outside the application directory.

## Downgrade

The normal installer blocks downgrades. Controlled rollback releases must use an explicitly supported mechanism.

## Uninstall

Windows uninstall metadata invokes:

```text
Link.Windows.Installer.exe --uninstall
```

Default uninstall removes application/shell integration while preserving local business/cache data, logs and backups.

Destructive local-data removal requires:

```text
--uninstall --purge-data
```

and an interactive warning/confirmation.

## 12 October 2026 — P3 release-source safety continuation

The install engine now verifies any **selected** updater, synchronisation,
device-bridge or diagnostics service binary is present as a REQUIRED item in
the staged release manifest **before** copying application files. Missing
service executables reject the install; no more silently skipped service
registration represented as a completed installation.

On successful installation, `InstalledState.ReleaseFilePaths` retains the
list of required release-owned files. A later **upgrade** compares the prior
inventory with the new manifest and removes only specific obsolete paths
inside the application directory, within the existing rollback checkpoint.
Legacy installs with no inventory do NOT blindly delete unknown files.
Tests were added to the source smoke suite; .NET/Windows tests have not run.

**Important:** The present manifest uses size + SHA-256 verification and a
fingerprint, but is NOT a cryptographically authenticated publisher-signed
update feed. True signed manifests, pinned signer trust, verified automatic
release download, an actual `Link.Updater.exe` service, blue/green switch,
auto-update user policy and OS/install acceptance are STILL MISSING. Do not
tell operators unattended updates work simply because `enableUpdater`
exists. This remains an ECO-V1-P3/P17 engineering blocker until implemented.
