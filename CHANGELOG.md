# Changelog

## 0.7.0-dev — 2026-09-26

### LWI-P3/14 — Installation Location & File Layout
- Added per-machine/current-user layout policy and canonical default paths.
- Added path, free-space, UNC, data separation and existing-directory validation.
- Added safe writable-data directory preparation hook.
- Implemented the HTML-referenced Install Location UI.

### LWI-P4/14 — Prerequisite Manager
- Added profile-aware prerequisite models and evaluator.
- Added Visual C++ runtime, optional WebView2, bundled SQLite and PostgreSQL inventory.
- Added signed-payload remediation states and prerequisite UI.

### LWI-P5/14 — PostgreSQL Detection
- Added registry and Program Files discovery.
- Added version, service, data directory, port and running-state detection.
- Added psql executable resolution.

### LWI-P6/14 — Database Setup
- Added four database deployment modes matching the canonical HTML.
- Added cloud/backend TCP reachability checks.
- Added PostgreSQL network and authenticated psql SELECT 1 validation.
- Added database setup planning and guided UI.

### LWI-P7/14 — Database Bootstrap & Security
- Added per-mode bootstrap/security plans.
- Enforced no production cloud DDL from workstation setup.
- Added signed migration, least-privilege, backup/checkpoint and explicit-grant contracts.
- Added Windows Credential Manager storage for validated PostgreSQL runtime secrets.

## 0.2.0-dev — 2026-09-26

### LWI-P0/14 — Repository & Architecture Foundation
- Established .NET 8 solution structure and project boundaries.
- Added logging and source validation foundations.
- Established manual final-EXE packaging policy.

### LWI-P1/14 — Installer Shell & Branding
- Implemented WPF shell based on the canonical installer HTML.
- Added the 12-step navigation rail and first three setup experiences.
- Standardised attribution to **By MeetWell Technologies**.

### LWI-P2/14 — Windows Environment Detection
- Added Windows build/edition, architecture, disk, memory, network and elevation checks.
- Added SQLite readiness state and local PostgreSQL detection.
- Added blocker/warning/information evaluation rules.
