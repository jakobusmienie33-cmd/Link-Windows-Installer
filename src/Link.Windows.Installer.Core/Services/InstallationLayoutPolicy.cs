using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class InstallationLayoutPolicy
{
    public const long MinimumFreeBytes = 1_300_000_000;

    public LayoutValidationResult Validate(
        InstallationLayout layout,
        long availableBytes,
        bool isAdministrator)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateDirectory("Application folder", layout.ApplicationDirectory, errors);
        ValidateDirectory("Data folder", layout.DataDirectory, errors);
        ValidateDirectory("SQLite/cache folder", layout.CacheDirectory, errors);
        ValidateDirectory("Log folder", layout.LogDirectory, errors);
        ValidateDirectory("Backup folder", layout.BackupDirectory, errors);

        if (availableBytes > 0 && availableBytes < MinimumFreeBytes)
            errors.Add("The selected application drive does not have the minimum 1.3 GB free space.");

        if (PathEquals(layout.ApplicationDirectory, layout.DataDirectory))
            errors.Add("Application files and mutable application data must not share the same directory.");

        if (IsChildOf(layout.DataDirectory, layout.ApplicationDirectory))
            warnings.Add("The data directory is inside the application directory. ProgramData is recommended for per-machine installs.");

        if (layout.PerMachine && !isAdministrator)
            warnings.Add("Per-machine installation requires elevation when files/services are applied. Setup may continue and request elevation later.");

        if (string.IsNullOrWhiteSpace(layout.StartMenuFolder))
            warnings.Add("No Start Menu folder is configured.");

        return new LayoutValidationResult(errors.Count == 0, errors, warnings);
    }

    private static void ValidateDirectory(string label, string path, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{label} is required.");
            return;
        }

        if (!Path.IsPathFullyQualified(path))
        {
            errors.Add($"{label} must be an absolute Windows path.");
            return;
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal))
            errors.Add($"{label} cannot use a UNC/network path for the standard installer profile.");

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            errors.Add($"{label} contains invalid path characters.");
    }

    private static bool PathEquals(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsChildOf(string candidate, string parent)
    {
        try
        {
            var childPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate)) + Path.DirectorySeparatorChar;
            var parentPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent)) + Path.DirectorySeparatorChar;
            return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
