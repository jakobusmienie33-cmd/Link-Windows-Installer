# Changelog

## 1.0.0-rc1 — 2026-09-26

### LWI-P8/14 — Application Deployment
- Added Link-Core release manifest and SHA-256/file-size validation.
- Added safe staged-path resolution and verified deployment engine.

### LWI-P9/14 — Configuration & Environment
- Added client-safe runtime configuration generation.
- Added persisted installation state and manifest fingerprinting.

### LWI-P10/14 — Windows Integration
- Added Start Menu/Desktop shortcut creation.
- Added optional startup registration and Add/Remove Programs metadata.
- Added manifest-driven service-registration hooks.

### LWI-P11/14 — Upgrade / Repair / Rollback
- Added install/upgrade/repair detection and downgrade blocking.
- Added application checkpoints and transactional rollback.

### LWI-P12/14 — Uninstall & Data Preservation
- Added real command-line uninstall execution.
- Added default business/cache/log/backup preservation.
- Added explicit destructive purge-data uninstall confirmation.

### LWI-P13/14 — Validation & Manual Packaging Readiness
- Expanded smoke tests across manifest, upgrade, repair, downgrade and uninstall policy.
- Added manifest generation, staging, release verification and manual packaging-readiness scripts.
- Completed all canonical installer UI screens.

## 0.7.0-dev — 2026-09-26

Completed LWI-P3 through LWI-P7: install layout, prerequisites, PostgreSQL discovery, database setup and database bootstrap/security.

## 0.2.0-dev — 2026-09-26

Completed LWI-P0 through LWI-P2: repository foundation, branded shell and Windows environment detection.
