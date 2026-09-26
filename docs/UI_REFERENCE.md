# Canonical Windows Installer UI Reference

The canonical reference is:

`the_link_windows_installer_comprehensive_mockup.html`

## Required visual language

Production WPF screens retain:
- dark The Link top system bar,
- dark left 12-step progress rail,
- The Link green accent/status language,
- white cards on the soft grey workspace,
- compact status pills/notices,
- Back / Next / Cancel footer controls,
- guided explanations suitable for the Finn Test,
- **By MeetWell Technologies** attribution.

## Implemented HTML-matched screens

### Step 1 — Welcome
Core application, local SQLite, optional branch PostgreSQL and device-service explanation.

### Step 2 — System Check
Live Windows compatibility checks rather than static sample values.

### Step 3 — Install Type
Standard Workstation, POS / Sharing Point, Back-Office / Admin and Branch Server / Advanced.

### Step 4 — Components
Component cards plus a live prerequisite inventory and signed-payload remediation state.

### Step 5 — Database / SQL
Four canonical database modes:
- Cloud Supabase + Local SQLite,
- Existing PostgreSQL,
- Install Local PostgreSQL,
- SQLite-only/offline preparation.

The production screen adds real discovery, endpoint tests, PostgreSQL authenticated self-test support and security-plan preview.

### Step 6 — Install Location
Canonical application/data/cache/log paths plus backup/checkpoint path, install scope and live layout validation.

## UI rule

HTML is the design reference; WPF is the production implementation. Behaviour must be backed by installer services rather than simulated HTML-only state.
