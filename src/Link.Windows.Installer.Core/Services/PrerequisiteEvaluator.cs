using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class PrerequisiteEvaluator
{
    public bool HasBlockingItems(IEnumerable<PrerequisiteCheck> checks) =>
        checks.Any(check => check.IsBlocking);

    public string Summarise(IEnumerable<PrerequisiteCheck> checks)
    {
        var snapshot = checks.ToArray();
        var blockers = snapshot.Count(check => check.IsBlocking);
        var installed = snapshot.Count(check =>
            check.State is PrerequisiteState.Installed or PrerequisiteState.Bundled);

        if (blockers > 0)
            return $"{blockers} required prerequisite(s) need attention before installation can be applied.";

        return $"{installed} prerequisite(s) are already available or bundled. Remaining planned items can be supplied from the signed installer payload.";
    }
}
