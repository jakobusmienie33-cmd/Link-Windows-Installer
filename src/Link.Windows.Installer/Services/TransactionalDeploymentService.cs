using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Services;

public sealed class TransactionalDeploymentService
{
    public async Task DeployAsync(
        string stagingDirectory,
        ReleaseManifest release,
        DeploymentPlan plan,
        IProgress<DeploymentProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = Path.Combine(
            plan.BackupDirectory,
            $"app-checkpoint-{DateTime.UtcNow:yyyyMMdd-HHmmss}");

        var applicationExists = Directory.Exists(plan.ApplicationDirectory);

        try
        {
            progress?.Report(new(5, "Preparing rollback", "Creating application checkpoint metadata."));
            Directory.CreateDirectory(plan.BackupDirectory);

            if (applicationExists)
                CopyDirectory(plan.ApplicationDirectory, checkpoint, cancellationToken);

            progress?.Report(new(20, "Preparing target", "Creating application directory."));
            Directory.CreateDirectory(plan.ApplicationDirectory);

            var requiredFiles = release.Files.Where(file => file.Required).ToArray();
            var total = Math.Max(requiredFiles.Length, 1);

            for (var index = 0; index < requiredFiles.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var file = requiredFiles[index];
                var source = ReleasePayloadService.ResolveSafePath(stagingDirectory, file.Path);
                var destination = ReleasePayloadService.ResolveSafePath(plan.ApplicationDirectory, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                File.Copy(source, destination, true);

                var percent = 20 + (int)Math.Round(((index + 1d) / total) * 65d);
                progress?.Report(new(percent, "Installing application files", file.Path));
                await Task.Yield();
            }

            progress?.Report(new(90, "Verifying deployment", "Confirming application entry point."));
            var entryPoint = ReleasePayloadService.ResolveSafePath(plan.ApplicationDirectory, release.EntryPoint);
            if (!File.Exists(entryPoint))
                throw new InvalidOperationException("Application entry point is missing after deployment.");

            progress?.Report(new(100, "Application deployment complete", release.EntryPoint));
        }
        catch
        {
            if (Directory.Exists(checkpoint))
            {
                if (Directory.Exists(plan.ApplicationDirectory))
                    Directory.Delete(plan.ApplicationDirectory, true);

                CopyDirectory(checkpoint, plan.ApplicationDirectory, CancellationToken.None);
            }
            else if (!applicationExists && Directory.Exists(plan.ApplicationDirectory))
            {
                Directory.Delete(plan.ApplicationDirectory, true);
            }

            throw;
        }
    }

    public void RepairFromPayload(
        string stagingDirectory,
        ReleaseManifest release,
        string applicationDirectory)
    {
        foreach (var file in release.Files.Where(file => file.Required))
        {
            var source = ReleasePayloadService.ResolveSafePath(stagingDirectory, file.Path);
            var destination = ReleasePayloadService.ResolveSafePath(applicationDirectory, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, true);
        }
    }

    private static void CopyDirectory(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var target = Path.Combine(destinationDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}
