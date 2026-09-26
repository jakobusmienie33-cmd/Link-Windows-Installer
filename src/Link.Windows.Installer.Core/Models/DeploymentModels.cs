namespace Link.Windows.Installer.Core.Models;

public enum DeploymentMode
{
    Install,
    Upgrade,
    Repair
}

public sealed record ReleaseFileEntry(
    string Path,
    string Sha256,
    long Size,
    bool Required = true);

public sealed record ReleaseManifest(
    string Product,
    string Version,
    string Architecture,
    string EntryPoint,
    IReadOnlyList<ReleaseFileEntry> Files,
    string Channel = "Stable",
    string MinimumInstallerVersion = "1.0.0");

public sealed record InstalledState(
    string Product,
    string Version,
    string Architecture,
    string EntryPoint,
    string ApplicationDirectory,
    string DataDirectory,
    string InstalledAtUtc,
    string ReleaseManifestSha256,
    string UpdateChannel,
    bool PerMachine);

public sealed record DeploymentPlan(
    DeploymentMode Mode,
    string TargetVersion,
    string ApplicationDirectory,
    string BackupDirectory,
    IReadOnlyList<string> FilesToCopy,
    IReadOnlyList<string> FilesToRemove,
    bool RequiresRollbackCheckpoint,
    string Summary);

public sealed record DeploymentProgress(
    int Percent,
    string Stage,
    string Detail);
