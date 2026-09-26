namespace Link.Windows.Installer.Core.Models;

public sealed record InstallationLayout(
    string ApplicationDirectory,
    string DataDirectory,
    string CacheDirectory,
    string LogDirectory,
    string BackupDirectory,
    bool PerMachine,
    string StartMenuFolder);
