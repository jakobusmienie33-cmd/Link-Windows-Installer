using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class DatabaseBootstrapPlanner
{
    public DatabaseBootstrapPlan Create(DatabaseSetupOptions options)
    {
        var commonControls = new List<string>
        {
            "No production Supabase service_role credential is stored on the workstation.",
            "Logs must redact database passwords, access tokens, PINs and payment secrets.",
            "Every schema change comes from a versioned signed migration bundle.",
            "Migration history and schema version are verified after execution.",
            "A backup/checkpoint is required before destructive local upgrades."
        };

        return options.Mode switch
        {
            DatabaseMode.CloudSupabaseWithLocalSqlite => new(
                options.Mode,
                new[]
                {
                    "Prepare the encrypted/local SQLite store used by Link-Core.",
                    "Apply the Link-Core local migration bundle.",
                    "Initialise local migration history and durable sync/outbox metadata.",
                    "Verify the configured backend endpoint without privileged cloud credentials.",
                    "Hand organisation/branch activation to the normal authenticated Link sign-in flow."
                },
                commonControls.Concat(new[]
                {
                    "Production PostgreSQL RLS/policies/explicit Data API grants remain centrally managed and are not mutated by this installer."
                }).ToArray(),
                false,
                false,
                "TheLink/Database/Cloud"),

            DatabaseMode.ExistingPostgreSql or DatabaseMode.InstallLocalPostgreSql => new(
                options.Mode,
                new[]
                {
                    "Verify that the target is an approved local/branch PostgreSQL deployment.",
                    "Acquire privileged migration credentials only for the bootstrap operation.",
                    "Create/verify the application database using the migration identity.",
                    "Apply the signed local/branch schema migration bundle transactionally.",
                    "Revoke PUBLIC access not explicitly required by the migration contract.",
                    "Apply explicit least-privilege grants to the dedicated runtime role.",
                    "Validate runtime read/write behaviour using the restricted role.",
                    "Persist only the runtime secret in Windows Credential Manager; discard bootstrap credentials."
                },
                commonControls.Concat(new[]
                {
                    "Runtime and migration identities are separate.",
                    "The runtime role must not receive SUPERUSER, CREATEDB, CREATEROLE or schema-owner privileges.",
                    "Explicit grants are applied by the migration bundle; legacy implicit grants are not relied on."
                }).ToArray(),
                false,
                true,
                "TheLink/Database/PostgreSQL"),

            DatabaseMode.SqliteOnly => new(
                options.Mode,
                new[]
                {
                    "Prepare the local SQLite store.",
                    "Apply the signed local migration bundle.",
                    "Initialise migration history and offline/sync metadata.",
                    "Record remote activation as pending."
                },
                commonControls,
                false,
                false,
                "TheLink/Database/SQLite"),

            _ => throw new InvalidOperationException("Unsupported database mode.")
        };
    }
}
