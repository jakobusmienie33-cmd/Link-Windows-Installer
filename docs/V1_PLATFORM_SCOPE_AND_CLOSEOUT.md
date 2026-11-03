# V1 Platform Scope and Closeout

**Master plan:** https://github.com/jakobusmienie33-cmd/Link-Core/blob/main/docs/2026-11-03_V1_PLATFORM_DEVELOPMENT_AND_CODING_SUPABASE_CLOSEOUT_MASTER_PLAN.md

This repository is a V1 supporting repository only for the current Windows installation and update path required to run the Link-Core V1 desktop experience. It does not bring the future Link OS roadmap into V1.

## V1 responsibilities
- Install and launch the supported V1 desktop application.
- Preserve required application configuration without embedding service-role keys or provider secrets.
- Support safe update, interruption recovery, rollback/forward recovery and clear operator errors.
- Preserve user data and configuration during upgrades.
- Provide accurate product attribution and version information.
- Link to the supported release source and configuration documentation.

## Error prevention
Handle missing runtime dependencies, insufficient permissions, interrupted downloads, checksum/signature mismatch, low disk space, locked files, partial installation, failed update, stale version, rollback failure and user-data migration failure. Never report installation success until the installer has completed its own operation and recorded the actual installed version.

## Working rules
Coding and Supabase only. No CI, tests or manual QA unless requested. Update installer UI, source, configuration and operator documentation together. Features beyond the current V1 desktop delivery requirement are V2.
