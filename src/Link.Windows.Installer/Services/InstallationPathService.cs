using System.IO;
using Link.Windows.Installer.Core.Models;
using Link.Windows.Installer.Core.Services;

namespace Link.Windows.Installer.Services;

public sealed class InstallationPathService
{
    private readonly InstallationLayoutPolicy _policy;

    public InstallationPathService(InstallationLayoutPolicy policy)
    {
        _policy = policy;
    }

    public InstallationLayout CreateDefaultLayout(bool perMachine = true)
    {
        var applicationRoot = perMachine
            ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var dataRoot = perMachine
            ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var application = Path.Combine(applicationRoot, "The Link");
        var data = Path.Combine(dataRoot, "The Link");

        return new InstallationLayout(
            application,
            data,
            Path.Combine(data, "Data"),
            Path.Combine(data, "Logs", "Setup"),
            Path.Combine(data, "Backups"),
            perMachine,
            "The Link");
    }

    public LayoutValidationResult Validate(InstallationLayout layout, bool isAdministrator)
    {
        var availableBytes = GetAvailableBytes(layout.ApplicationDirectory);
        var baseResult = _policy.Validate(layout, availableBytes, isAdministrator);
        var errors = baseResult.Errors.ToList();
        var warnings = baseResult.Warnings.ToList();

        foreach (var directory in new[]
                 {
                     layout.ApplicationDirectory,
                     layout.DataDirectory,
                     layout.CacheDirectory,
                     layout.LogDirectory,
                     layout.BackupDirectory
                 })
        {
            if (Directory.Exists(directory))
                warnings.Add($"Existing directory detected and will be preserved/reused where compatible: {directory}");
        }

        return new LayoutValidationResult(errors.Count == 0, errors, warnings);
    }

    public IReadOnlyList<string> PrepareWritableDataDirectories(InstallationLayout layout)
    {
        var created = new List<string>();

        foreach (var directory in new[]
                 {
                     layout.DataDirectory,
                     layout.CacheDirectory,
                     layout.LogDirectory,
                     layout.BackupDirectory
                 })
        {
            if (Directory.Exists(directory))
                continue;

            Directory.CreateDirectory(directory);
            created.Add(directory);
        }

        return created;
    }

    private static long GetAvailableBytes(string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            return string.IsNullOrWhiteSpace(root) ? 0 : new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }
}
