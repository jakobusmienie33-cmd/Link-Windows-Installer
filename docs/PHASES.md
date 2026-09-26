# Link Windows Installer — Phase Status

All planned Windows installer phases are complete at source level. Final EXE packaging remains manual.

## LWI-P0/14 — Repository & Architecture Foundation
**Status: Complete**

## LWI-P1/14 — Installer Shell & Branding
**Status: Complete**

Canonical HTML-based WPF shell, 12-step rail, Link visual language, and **By MeetWell Technologies** attribution.

## LWI-P2/14 — Windows Environment Detection
**Status: Complete**

Windows build/edition, x64/ARM64, disk, memory, network, elevation, SQLite readiness and PostgreSQL baseline detection.

## LWI-P3/14 — Installation Location & File Layout
**Status: Complete**

Program Files / ProgramData defaults, current-user option, data/cache/log/backup separation, free-space/path validation and existing-directory awareness.

## LWI-P4/14 — Prerequisite Manager
**Status: Complete**

Profile-aware prerequisite inventory, VC++ runtime detection, optional WebView2, bundled SQLite and signed-payload remediation states.

## LWI-P5/14 — PostgreSQL Detection
**Status: Complete**

Registry/Program Files discovery, version/service/port/data path state and psql resolution.

## LWI-P6/14 — Database Setup
**Status: Complete**

Cloud + SQLite, existing PostgreSQL, local PostgreSQL and SQLite-only modes with connectivity/self-test support.

## LWI-P7/14 — Database Bootstrap & Security
**Status: Complete**

No production cloud DDL, no service_role storage, signed migration contract, runtime/migration identity separation, explicit grants, backup checkpoints and Windows Credential Manager.

## LWI-P8/14 — Application Deployment
**Status: Complete**

Verified Link-Core release-manifest contract, staged payload hashing/size checks, safe paths and application deployment engine.

## LWI-P9/14 — Configuration & Environment
**Status: Complete**

Client-safe runtime configuration generation under ProgramData/LocalAppData with privileged-secret guard and persisted install state.

## LWI-P10/14 — Windows Integration
**Status: Complete**

Start Menu/Desktop shortcuts, optional startup registration, Add/Remove Programs metadata and manifest-driven Windows service-registration hooks.

## LWI-P11/14 — Upgrade / Repair / Rollback
**Status: Complete**

Install/upgrade/repair mode detection, downgrade blocking, application checkpoints and transactional rollback on failed deployment.

## LWI-P12/14 — Uninstall & Data Preservation
**Status: Complete**

Real --uninstall command path, shortcut/service/startup/metadata cleanup, business/cache/log/backup preservation by default, explicit --purge-data destructive path with confirmation.

## LWI-P13/14 — Validation & Manual Packaging Readiness
**Status: Complete**

Extended smoke tests, release manifest tooling, staging/hash verification scripts, source/build gates and manual packaging acceptance matrix.

## Release boundary

Installer source target: **1.0.0-rc1**

The final production EXE is created and signed manually by MeetWell Technologies.
