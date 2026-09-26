using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class DatabaseSetupPlanner
{
    public IReadOnlyList<string> Validate(DatabaseSetupOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.LocalCacheDirectory))
            errors.Add("A local SQLite/cache directory is required.");

        switch (options.Mode)
        {
            case DatabaseMode.CloudSupabaseWithLocalSqlite:
                if (!Uri.TryCreate(options.BackendUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                    errors.Add("A valid HTTPS backend URL is required for cloud mode.");
                else if (options.RequireTls && uri.Scheme != Uri.UriSchemeHttps)
                    errors.Add("HTTPS is required for the configured cloud backend.");
                break;

            case DatabaseMode.ExistingPostgreSql:
                ValidatePostgreSqlFields(options, errors);
                break;

            case DatabaseMode.InstallLocalPostgreSql:
                if (string.IsNullOrWhiteSpace(options.DatabaseName))
                    errors.Add("A local PostgreSQL database name is required.");
                if (string.IsNullOrWhiteSpace(options.UserName))
                    errors.Add("A restricted PostgreSQL runtime user name is required.");
                break;

            case DatabaseMode.SqliteOnly:
                break;

            default:
                errors.Add("Unsupported database mode.");
                break;
        }

        return errors;
    }

    public DatabaseSetupPlan CreatePlan(DatabaseSetupOptions options)
    {
        var errors = Validate(options);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        return options.Mode switch
        {
            DatabaseMode.CloudSupabaseWithLocalSqlite => new(
                options.Mode,
                "Cloud Supabase + Local SQLite",
                "Recommended workstation mode. The cloud backend remains authoritative while SQLite provides approved local cache/offline queues.",
                new[]
                {
                    "Create/verify the local SQLite data directory.",
                    "Apply the signed local SQLite migration bundle.",
                    "Store only public/client-safe backend configuration.",
                    "Verify outbound backend reachability.",
                    "Leave production cloud schema migrations to the centrally managed deployment pipeline."
                },
                new[]
                {
                    "The installer must never store a production Supabase service_role secret.",
                    "Cloud DDL is not executed by ordinary Windows workstation setup."
                },
                true,
                false,
                false),

            DatabaseMode.ExistingPostgreSql => new(
                options.Mode,
                "Existing PostgreSQL",
                "Advanced branch/server mode using an approved existing PostgreSQL instance.",
                new[]
                {
                    "Resolve the selected PostgreSQL host/instance.",
                    "Verify TCP and authenticated database connectivity.",
                    "Verify supported server version and required local extensions.",
                    "Create a migration checkpoint/backup before schema change.",
                    "Apply only the signed local/branch migration bundle.",
                    "Store the runtime credential in Windows Credential Manager."
                },
                new[]
                {
                    "Do not point this mode at the production Supabase database from an ordinary workstation.",
                    "Use a dedicated restricted runtime role; never reuse a database superuser for application runtime."
                },
                true,
                true,
                true),

            DatabaseMode.InstallLocalPostgreSql => new(
                options.Mode,
                "Install Local PostgreSQL",
                "Advanced branch/server mode. A signed approved PostgreSQL package is installed before local database bootstrap.",
                new[]
                {
                    "Verify the PostgreSQL package signature/checksum from the installer payload.",
                    "Install PostgreSQL only when explicitly selected.",
                    "Create a dedicated local service/database identity.",
                    "Create the Link local database and restricted runtime role.",
                    "Apply signed local/branch migrations transactionally.",
                    "Run read/write/schema self-tests and register backup policy."
                },
                new[]
                {
                    "Local PostgreSQL is not a normal workstation prerequisite.",
                    "The package is sourced from the signed installer payload; the installer does not silently download arbitrary database binaries."
                },
                false,
                true,
                true),

            DatabaseMode.SqliteOnly => new(
                options.Mode,
                "Local SQLite Only / Offline Preparation",
                "Creates the local data layer and defers remote activation until a later authenticated online setup.",
                new[]
                {
                    "Create/verify the local SQLite data directory.",
                    "Apply signed local migrations.",
                    "Initialise durable sync/outbox metadata.",
                    "Mark remote activation as pending."
                },
                new[]
                {
                    "Authoritative online workflows remain unavailable until backend activation succeeds.",
                    "Payments and server-authoritative stock/compliance operations must respect offline restrictions."
                },
                false,
                false,
                false),

            _ => throw new InvalidOperationException("Unsupported database mode.")
        };
    }

    private static void ValidatePostgreSqlFields(
        DatabaseSetupOptions options,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
            errors.Add("PostgreSQL host is required.");
        if (options.Port is < 1 or > 65535)
            errors.Add("PostgreSQL port must be between 1 and 65535.");
        if (string.IsNullOrWhiteSpace(options.DatabaseName))
            errors.Add("PostgreSQL database name is required.");
        if (string.IsNullOrWhiteSpace(options.UserName))
            errors.Add("PostgreSQL runtime user name is required.");
    }
}
