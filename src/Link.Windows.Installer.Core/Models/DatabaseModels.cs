namespace Link.Windows.Installer.Core.Models;

public enum DatabaseMode
{
    CloudSupabaseWithLocalSqlite,
    ExistingPostgreSql,
    InstallLocalPostgreSql,
    SqliteOnly
}

public enum DatabaseValidationState
{
    Valid,
    Warning,
    Invalid,
    Pending
}

public sealed record DatabaseSetupOptions(
    DatabaseMode Mode,
    string BackendUrl,
    string Host,
    int Port,
    string DatabaseName,
    string UserName,
    string LocalCacheDirectory,
    string PostgreSqlInstanceId = "",
    bool RequireTls = true);

public sealed record DatabaseSetupPlan(
    DatabaseMode Mode,
    string Title,
    string Summary,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Warnings,
    bool RequiresNetwork,
    bool RequiresPostgreSql,
    bool AllowsPrivilegedLocalBootstrap);

public sealed record DatabaseValidationResult(
    DatabaseValidationState State,
    string Summary,
    string Detail)
{
    public bool IsSuccessful => State == DatabaseValidationState.Valid;
}

public sealed record DatabaseBootstrapPlan(
    DatabaseMode Mode,
    IReadOnlyList<string> OrderedActions,
    IReadOnlyList<string> SecurityControls,
    bool RunsProductionCloudDdl,
    bool RequiresPrivilegedExecutor,
    string CredentialReferencePrefix);
