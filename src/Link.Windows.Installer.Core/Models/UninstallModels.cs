namespace Link.Windows.Installer.Core.Models;

public sealed record UninstallOptions(
    bool RemoveApplicationFiles,
    bool RemoveShortcuts,
    bool RemoveStartupRegistration,
    bool RemoveInstallerMetadata,
    bool PreserveBusinessData,
    bool PreserveLogs,
    bool PreserveBackups);

public sealed record UninstallPlan(
    IReadOnlyList<string> RemovePaths,
    IReadOnlyList<string> PreservePaths,
    IReadOnlyList<string> ShellActions,
    IReadOnlyList<string> Warnings);
