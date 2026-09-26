using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Services;

public sealed class UninstallExecutionService
{
    public void Execute(UninstallPlan plan)
    {
        foreach (var path in plan.RemovePaths)
        {
            if (plan.PreservePaths.Any(preserved => IsSameOrChild(path, preserved)))
                continue;

            if (File.Exists(path))
                File.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    private static bool IsSameOrChild(string candidate, string preserved)
    {
        var c = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar);
        var p = Path.GetFullPath(preserved).TrimEnd(Path.DirectorySeparatorChar);
        return c.Equals(p, StringComparison.OrdinalIgnoreCase) ||
               c.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
