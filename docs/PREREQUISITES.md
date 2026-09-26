# Prerequisite Management

The prerequisite manager inventories the machine before installation and records one of these states:

- **Installed** — compatible runtime/component already detected.
- **Bundled** — provided by The Link application/runtime.
- **Planned** — required but intentionally supplied later from the signed installer payload.
- **Optional** — not required for the current profile.
- **Missing** — required and no approved remediation is currently available.
- **Unknown** — detection could not establish state.

Current probes include:
- The Link Windows payload staging contract,
- bundled SQLite,
- Microsoft Visual C++ x64 runtime registration,
- optional WebView2 runtime,
- PostgreSQL for advanced branch/server profiles.

The installer does not silently fetch arbitrary third-party binaries. Prerequisite installation will use signed staged payloads or explicitly approved organisation-managed sources.
