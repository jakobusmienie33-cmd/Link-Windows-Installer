using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class SystemCompatibilityEvaluator
{
    public const long MinimumDiskBytes = 1_300_000_000;
    public const long MinimumMemoryBytes = 4L * 1024 * 1024 * 1024;
    public const long RecommendedMemoryBytes = 8L * 1024 * 1024 * 1024;
    public const int MinimumWindows10Build = 17763;

    public IReadOnlyList<SystemCheckResult> Evaluate(SystemSnapshot snapshot)
    {
        return new List<SystemCheckResult>
        {
            EvaluateOperatingSystem(snapshot),
            EvaluateArchitecture(snapshot),
            EvaluateDisk(snapshot),
            EvaluateMemory(snapshot),
            new(
                "Network",
                snapshot.IsNetworkAvailable
                    ? "A network interface is available. Backend endpoint connectivity is validated in a later phase."
                    : "No active network interface detected. Offline-safe setup can continue, but cloud activation will require connectivity.",
                snapshot.IsNetworkAvailable ? "Online" : "Offline",
                snapshot.IsNetworkAvailable ? SystemCheckState.Pass : SystemCheckState.Warning,
                "NET"),
            new(
                "Administrator",
                snapshot.IsAdministrator
                    ? "Setup is currently elevated."
                    : "Setup is not elevated. Windows may request administrator approval only when per-machine changes are applied.",
                snapshot.IsAdministrator ? "Elevated" : "As needed",
                SystemCheckState.Info,
                "ADM"),
            new(
                "SQLite",
                "Embedded local application data layer requires no separate database server installation.",
                "Bundled",
                SystemCheckState.Pass,
                "DB"),
            new(
                "PostgreSQL",
                snapshot.HasPostgreSql
                    ? snapshot.PostgreSqlDescription
                    : "No local PostgreSQL installation detected. This is normal for standard workstations.",
                snapshot.HasPostgreSql ? "Detected" : "Not Required",
                SystemCheckState.Info,
                "PG")
        };
    }

    private static SystemCheckResult EvaluateOperatingSystem(SystemSnapshot snapshot)
    {
        if (!snapshot.IsWindows)
            return new("Operating System", snapshot.OperatingSystem, "Unsupported", SystemCheckState.Block, "OS");

        if (snapshot.WindowsBuild < MinimumWindows10Build)
            return new(
                "Operating System",
                $"{snapshot.OperatingSystem} (build {snapshot.WindowsBuild}). Windows 10 build {MinimumWindows10Build} or later is required.",
                "Upgrade Required",
                SystemCheckState.Block,
                "OS");

        return new(
            "Operating System",
            $"{snapshot.OperatingSystem} • build {snapshot.WindowsBuild}",
            "Supported",
            SystemCheckState.Pass,
            "✓");
    }

    private static SystemCheckResult EvaluateArchitecture(SystemSnapshot snapshot)
    {
        var supported = snapshot.Architecture.Equals("X64", StringComparison.OrdinalIgnoreCase) ||
                        snapshot.Architecture.Equals("Arm64", StringComparison.OrdinalIgnoreCase);

        return new(
            "Architecture",
            $"{snapshot.Architecture} operating system architecture detected.",
            supported ? "Compatible" : "Unsupported",
            supported ? SystemCheckState.Pass : SystemCheckState.Block,
            supported ? "✓" : "CPU");
    }

    private static SystemCheckResult EvaluateDisk(SystemSnapshot snapshot)
    {
        var freeGb = snapshot.FreeDiskBytes / 1_000_000_000d;
        var enough = snapshot.FreeDiskBytes >= MinimumDiskBytes;

        return new(
            "Disk Space",
            $"{freeGb:0.0} GB free • estimated 1.3 GB normal workstation installation.",
            enough ? "Enough" : "Insufficient",
            enough ? SystemCheckState.Pass : SystemCheckState.Block,
            enough ? "✓" : "DISK");
    }

    private static SystemCheckResult EvaluateMemory(SystemSnapshot snapshot)
    {
        var memoryGb = snapshot.PhysicalMemoryBytes / 1024d / 1024d / 1024d;

        if (snapshot.PhysicalMemoryBytes < MinimumMemoryBytes)
            return new(
                "Memory",
                $"{memoryGb:0.0} GB installed. A minimum of 4 GB is required.",
                "Insufficient",
                SystemCheckState.Block,
                "RAM");

        if (snapshot.PhysicalMemoryBytes < RecommendedMemoryBytes)
            return new(
                "Memory",
                $"{memoryGb:0.0} GB installed. 8 GB or more is recommended.",
                "Supported",
                SystemCheckState.Warning,
                "RAM");

        return new(
            "Memory",
            $"{memoryGb:0.0} GB installed.",
            "Recommended",
            SystemCheckState.Pass,
            "✓");
    }
}
