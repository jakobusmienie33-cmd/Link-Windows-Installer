using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class AutomaticUpdatePlanner
{
    public ClientUpdatePlan Plan(
        ReleaseUpdateDirective release,
        ClientUpdateSnapshot installed)
    {
        if (release.Status == ReleaseDirectiveStatus.Withdrawn)
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.Blocked,
                null,
                false,
                new[] { "Release has been withdrawn by Release Control." });
        }

        if (release.Status == ReleaseDirectiveStatus.Paused)
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.None,
                null,
                false,
                new[] { "Release rollout is paused." });
        }

        if (release.Status == ReleaseDirectiveStatus.Rollback)
            return PlanRollback(release, installed);

        if (CompareVersions(installed.UpdaterVersion, release.MinimumUpdaterVersion) < 0)
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.Blocked,
                release.Version,
                false,
                new[]
                {
                    $"Updater {installed.UpdaterVersion} is older than required {release.MinimumUpdaterVersion}."
                });
        }

        if (CompareVersions(release.Version, installed.CurrentVersion) <= 0)
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.None,
                null,
                false,
                new[] { "Installed application version is current or newer." });
        }

        if (installed.Policy == ClientUpdatePolicy.Manual)
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.NotifyOnly,
                release.Version,
                true,
                new[] { "Manual update policy requires authorised approval." });
        }

        var staged = string.Equals(
            installed.StagedVersion,
            release.Version,
            StringComparison.OrdinalIgnoreCase);

        var activationAllowed = !installed.CriticalWorkflowActive &&
            (installed.Policy != ClientUpdatePolicy.Scheduled ||
             installed.WithinMaintenanceWindow);

        if (staged && activationAllowed)
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.ActivateStaged,
                release.Version,
                false,
                new[] { "Candidate is staged and the client is at a safe activation point." });
        }

        var reasons = new List<string>
        {
            staged
                ? "Candidate is already staged."
                : "Candidate must be downloaded, verified and staged."
        };

        if (installed.CriticalWorkflowActive)
            reasons.Add("Activation is deferred because a protected workflow is active.");

        if (installed.Policy == ClientUpdatePolicy.Scheduled &&
            !installed.WithinMaintenanceWindow)
            reasons.Add("Activation is deferred until the configured maintenance window.");

        return new ClientUpdatePlan(
            ClientUpdateAction.DownloadAndStage,
            release.Version,
            !activationAllowed,
            reasons);
    }

    private static ClientUpdatePlan PlanRollback(
        ReleaseUpdateDirective release,
        ClientUpdateSnapshot installed)
    {
        var target = release.RollbackTargetVersion;
        if (string.IsNullOrWhiteSpace(target))
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.Blocked,
                null,
                false,
                new[] { "Rollback directive is missing a target version." });
        }

        if (string.Equals(installed.CurrentVersion, target, StringComparison.OrdinalIgnoreCase))
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.None,
                target,
                false,
                new[] { "Client is already on the rollback target." });
        }

        if (!string.Equals(installed.PreviousVersion, target, StringComparison.OrdinalIgnoreCase))
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.Blocked,
                target,
                false,
                new[] { "Rollback target is not the locally retained previous version." });
        }

        if (installed.CriticalWorkflowActive)
        {
            return new ClientUpdatePlan(
                ClientUpdateAction.Rollback,
                target,
                true,
                new[] { "Rollback is required but activation waits for the protected workflow to finish." });
        }

        return new ClientUpdatePlan(
            ClientUpdateAction.Rollback,
            target,
            false,
            new[] { "Release Control requires immediate rollback to the retained known-good version." });
    }

    private static int CompareVersions(string left, string right)
    {
        var a = Parse(left);
        var b = Parse(right);
        return a.CompareTo(b);
    }

    private static Version Parse(string value)
    {
        var dash = value.IndexOf('-');
        var normalised = dash > 0 ? value[..dash] : value;
        return Version.TryParse(normalised, out var version)
            ? version
            : throw new InvalidOperationException($"Invalid release version: {value}");
    }
}
