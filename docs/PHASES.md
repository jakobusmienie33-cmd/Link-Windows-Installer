# Link Windows Installer — Phase Status

## LWI-P0/14 — Repository & Architecture Foundation
**Status: Complete**

Solution structure, architecture boundary, versioning, logging, validation and manual packaging policy established.

## LWI-P1/14 — Installer Shell & Branding
**Status: Complete**

The WPF shell follows the canonical installer HTML: 12-step rail, Link header/sidebar, guided cards, Back/Next/Cancel controls and **By MeetWell Technologies** attribution.

## LWI-P2/14 — Windows Environment Detection
**Status: Complete**

Windows 10/11 build, x64/ARM64, disk, memory, network, elevation, SQLite readiness and PostgreSQL baseline detection implemented.

## LWI-P3/14 — Installation Location & File Layout
**Status: Complete**

- Canonical Program Files / ProgramData defaults.
- Per-machine and current-user layouts.
- Application/data/cache/log/backup separation.
- Absolute-path, free-space, UNC and collision validation.
- Existing-directory preservation awareness.
- Data-directory preparation hook for the install engine.
- Install Location UI implemented from the canonical HTML.

## LWI-P4/14 — Prerequisite Manager
**Status: Complete**

- Profile-aware prerequisite inventory.
- Signed-payload planning model.
- Visual C++ runtime discovery.
- WebView2 optional-runtime discovery.
- Bundled SQLite state.
- PostgreSQL optional/advanced prerequisite state.
- Components + prerequisite manager UI implemented.

## LWI-P5/14 — PostgreSQL Detection
**Status: Complete**

- Registry discovery across 32/64-bit views.
- Program Files fallback discovery.
- Version, base/data directory, service ID, port and service-running state.
- postgresql.conf port parsing.
- psql.exe resolution.
- Database UI exposes discovered installations.

## LWI-P6/14 — Database Setup
**Status: Complete**

Supported modes:
1. Cloud Supabase/PostgreSQL + local SQLite (normal workstation default).
2. Existing approved PostgreSQL.
3. Installer-managed local PostgreSQL (advanced branch/server).
4. SQLite-only/offline preparation.

Includes:
- Configuration validation.
- Database setup planning.
- TCP cloud/backend reachability.
- PostgreSQL TCP validation.
- Authenticated `SELECT 1` validation through detected `psql.exe` when credentials are supplied.
- Database mode/configuration UI.

## LWI-P7/14 — Database Bootstrap & Security
**Status: Complete**

- Explicit bootstrap plans per database mode.
- Production cloud DDL invariant: prohibited from workstation installer.
- Signed/versioned migration-bundle contract.
- Local/branch PostgreSQL runtime vs migration identity separation.
- Least-privilege explicit grant contract.
- Backup/checkpoint requirement before destructive local migrations.
- Windows Credential Manager storage for validated PostgreSQL runtime secrets.
- Secret-redaction and credential-reference contract.

## Next
**LWI-P8/14 — Application Deployment**
