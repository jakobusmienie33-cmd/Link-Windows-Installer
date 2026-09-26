namespace Link.Windows.Installer.Core.Models;

public sealed record SystemSnapshot(
    bool IsWindows,
    string OperatingSystem,
    int WindowsBuild,
    string Architecture,
    long FreeDiskBytes,
    long PhysicalMemoryBytes,
    bool IsNetworkAvailable,
    bool IsAdministrator,
    bool HasPostgreSql,
    string PostgreSqlDescription);
