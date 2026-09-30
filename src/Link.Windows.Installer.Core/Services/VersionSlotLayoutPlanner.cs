using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class VersionSlotLayoutPlanner
{
    public VersionSlotLayout Create(
        string applicationDirectory,
        string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(applicationDirectory))
            throw new ArgumentException("Application directory is required.", nameof(applicationDirectory));
        if (string.IsNullOrWhiteSpace(dataDirectory))
            throw new ArgumentException("Data directory is required.", nameof(dataDirectory));

        var appRoot = Path.GetFullPath(applicationDirectory);
        var updateState = Path.Combine(Path.GetFullPath(dataDirectory), "Updater");

        return new VersionSlotLayout(
            appRoot,
            Path.Combine(appRoot, "Launcher"),
            Path.Combine(appRoot, "Updater"),
            Path.Combine(appRoot, "Versions"),
            updateState,
            Path.Combine(updateState, "Staging"),
            Path.Combine(updateState, "current-version.txt"),
            Path.Combine(updateState, "previous-version.txt"));
    }
}
