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


## Automatic background upgrade

The production target no longer requires users to uninstall/reinstall for normal updates. Link-Core Release Control resolves the approved release; the Windows updater downloads, verifies and stages it in the background. Activation happens only when the client is safe, or inside the configured maintenance window for Scheduled policy.

The stable launcher/updater and version-slot migration is tracked in LWI-P15/18–LWI-P17/18. Until that work is complete, the existing transactional application checkpoint remains the active rollback mechanism.

Database and local-state changes must remain compatible with the retained previous application version during the rollback window.
