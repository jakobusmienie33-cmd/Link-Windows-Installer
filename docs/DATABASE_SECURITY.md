# Database Bootstrap & Security Contract

## Workstation invariant

The normal Windows workstation installer:
- uses local SQLite for approved cache/offline state,
- connects to the configured Link/Supabase backend with client-safe configuration,
- does not store a production Supabase `service_role` secret,
- does not execute production cloud DDL,
- does not grant itself database administration rights.

## Local / branch PostgreSQL

Advanced branch/server modes must:
1. use an approved existing server or a signed installer-supplied PostgreSQL package;
2. separate migration/bootstrap credentials from runtime credentials;
3. execute only versioned signed migrations;
4. make a backup/checkpoint before destructive migration steps;
5. revoke unintended PUBLIC access;
6. explicitly grant only runtime privileges required by the migration contract;
7. never grant the runtime role SUPERUSER, CREATEDB, CREATEROLE or ownership solely for convenience;
8. run a restricted-role read/write/schema self-test after migration;
9. store only the required runtime secret in Windows Credential Manager;
10. discard bootstrap/admin credentials after the operation.

## Supabase explicit grants

The Link platform's explicit-grant standard applies to centrally managed production migrations. New exposed-schema objects must define their RLS/policy/grant contract in the same migration and follow least privilege.

The Windows installer **does not** replace that deployment pipeline. Its local branch PostgreSQL migrations follow the same explicit-grant principle without pretending a workstation is authorised to administer production Supabase.

## Logging

Installer logs must never include:
- passwords or PINs,
- database administrator passwords,
- bearer/refresh tokens,
- Supabase service-role keys,
- payment secrets or full card data.

Logs may include sanitised endpoint names, versions, migration IDs, schema versions and non-secret error context.
