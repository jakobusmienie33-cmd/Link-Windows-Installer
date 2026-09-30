namespace Link.Windows.Installer.Core.Models;

public enum ClientUpdatePolicy
{
    Automatic,
    Scheduled,
    Manual
}

public enum ReleaseDirectiveStatus
{
    Candidate,
    Active,
    Paused,
    Withdrawn,
    Rollback
}

public enum ClientUpdateAction
{
    None,
    NotifyOnly,
    DownloadAndStage,
    ActivateStaged,
    Rollback,
    Blocked
}

public sealed record VersionSlotLayout(
    string ApplicationRoot,
    string LauncherDirectory,
    string UpdaterDirectory,
    string VersionsDirectory,
    string UpdateStateDirectory,
    string StagingDirectory,
    string CurrentPointerFile,
    string PreviousPointerFile);

public sealed record ReleaseUpdateDirective(
    string Version,
    ReleaseDirectiveStatus Status,
    string MinimumUpdaterVersion,
    string? RollbackTargetVersion = null);

public sealed record ClientUpdateSnapshot(
    string CurrentVersion,
    string? PreviousVersion,
    string UpdaterVersion,
    ClientUpdatePolicy Policy,
    bool CriticalWorkflowActive,
    bool WithinMaintenanceWindow,
    string? StagedVersion = null);

public sealed record ClientUpdatePlan(
    ClientUpdateAction Action,
    string? TargetVersion,
    bool ActivationDeferred,
    IReadOnlyList<string> Reasons);
