using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class UninstallPlanner
{
    public UninstallPlan Create(
        InstallationLayout layout,
        UninstallOptions options)
    {
        var remove = new List<string>();
        var preserve = new List<string>();
        var shell = new List<string>();
        var warnings = new List<string>();

        if (options.RemoveApplicationFiles)
            remove.Add(layout.ApplicationDirectory);

        if (options.PreserveBusinessData)
            preserve.Add(layout.DataDirectory);
        else
        {
            remove.Add(layout.DataDirectory);
            warnings.Add("Business data removal is destructive and must require explicit user confirmation.");
        }

        if (options.PreserveLogs)
            preserve.Add(layout.LogDirectory);

        if (options.PreserveBackups)
            preserve.Add(layout.BackupDirectory);

        if (options.RemoveShortcuts)
            shell.Add("Remove Start Menu and desktop shortcuts.");

        if (options.RemoveStartupRegistration)
            shell.Add("Remove The Link startup registration.");

        if (options.RemoveInstallerMetadata)
            shell.Add("Remove Add/Remove Programs metadata and installer state.");

        return new UninstallPlan(
            remove.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            preserve.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            shell,
            warnings);
    }
}
