using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class DeploymentPlanner
{
    public DeploymentPlan Plan(
        ReleaseManifest release,
        InstalledState? installed,
        string applicationDirectory,
        string backupDirectory)
    {
        var mode = DetermineMode(release, installed);

        var filesToCopy = release.Files
            .Where(file => file.Required)
            .Select(file => file.Path)
            .ToArray();

        var releaseSet = new HashSet<string>(
            release.Files.Select(file => Normalise(file.Path)),
            StringComparer.OrdinalIgnoreCase);

        var filesToRemove = Array.Empty<string>();

        return new DeploymentPlan(
            mode,
            release.Version,
            applicationDirectory,
            backupDirectory,
            filesToCopy,
            filesToRemove,
            installed is not null,
            mode switch
            {
                DeploymentMode.Install => $"Install The Link {release.Version}.",
                DeploymentMode.Upgrade => $"Upgrade The Link from {installed!.Version} to {release.Version}.",
                DeploymentMode.Repair => $"Repair The Link {release.Version} using the signed release payload.",
                _ => "Deploy The Link."
            });
    }

    public DeploymentMode DetermineMode(ReleaseManifest release, InstalledState? installed)
    {
        if (installed is null)
            return DeploymentMode.Install;

        if (string.Equals(installed.Version, release.Version, StringComparison.OrdinalIgnoreCase))
            return DeploymentMode.Repair;

        var current = Parse(installed.Version);
        var incoming = Parse(release.Version);

        if (incoming < current)
            throw new InvalidOperationException(
                $"Downgrade from {installed.Version} to {release.Version} is not allowed by the standard installer.");

        return DeploymentMode.Upgrade;
    }

    private static Version Parse(string value)
    {
        var dash = value.IndexOf('-');
        var normalised = dash > 0 ? value[..dash] : value;
        return Version.TryParse(normalised, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Invalid release version: {value}");
    }

    private static string Normalise(string path) => path.Replace('\\', '/').TrimStart('/');
}
