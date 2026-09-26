using System.Diagnostics;

namespace Link.Windows.Installer.Services;

public sealed class WindowsServiceRegistrationService
{
    public void RegisterSelectedServices(
        string applicationDirectory,
        bool deviceBridge,
        bool syncService,
        bool updater,
        bool diagnostics)
    {
        if (!OperatingSystem.IsWindows()) return;

        var services = new[]
        {
            new ServiceSpec("TheLinkDeviceBridge", "The Link Device Bridge", Path.Combine(applicationDirectory, "services", "device-bridge", "Link.DeviceBridge.exe"), deviceBridge, "auto"),
            new ServiceSpec("TheLinkSync", "The Link Sync Service", Path.Combine(applicationDirectory, "services", "sync", "Link.SyncService.exe"), syncService, "auto"),
            new ServiceSpec("TheLinkUpdater", "The Link Updater", Path.Combine(applicationDirectory, "services", "updater", "Link.Updater.exe"), updater, "delayed-auto"),
            new ServiceSpec("TheLinkDiagnostics", "The Link Diagnostics", Path.Combine(applicationDirectory, "services", "diagnostics", "Link.Diagnostics.exe"), diagnostics, "demand")
        };

        foreach (var service in services.Where(service => service.Enabled && File.Exists(service.Executable)))
            EnsureService(service);
    }

    public void RemoveKnownServices()
    {
        if (!OperatingSystem.IsWindows()) return;

        foreach (var name in new[] { "TheLinkDeviceBridge", "TheLinkSync", "TheLinkUpdater", "TheLinkDiagnostics" })
        {
            RunSc($"stop \"{name}\"", tolerateFailure: true);
            RunSc($"delete \"{name}\"", tolerateFailure: true);
        }
    }

    private static void EnsureService(ServiceSpec spec)
    {
        var query = RunSc($"query \"{spec.Name}\"", tolerateFailure: true);
        if (query.ExitCode != 0)
        {
            var create = $"create \"{spec.Name}\" binPath= \"{spec.Executable}\" DisplayName= \"{spec.DisplayName}\" start= {MapStart(spec.StartMode)}";
            RunSc(create, tolerateFailure: false);
        }

        if (spec.StartMode == "delayed-auto")
            RunSc($"config \"{spec.Name}\" start= delayed-auto", tolerateFailure: false);

        if (spec.StartMode is "auto" or "delayed-auto")
            RunSc($"start \"{spec.Name}\"", tolerateFailure: true);
    }

    private static string MapStart(string mode) => mode switch
    {
        "auto" => "auto",
        "delayed-auto" => "auto",
        _ => "demand"
    };

    private static ProcessResult RunSc(string arguments, bool tolerateFailure)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Could not start Windows Service Control.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(15000);

        if (!tolerateFailure && process.ExitCode != 0)
            throw new InvalidOperationException($"Windows service operation failed: {stderr} {stdout}".Trim());

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private sealed record ServiceSpec(
        string Name,
        string DisplayName,
        string Executable,
        bool Enabled,
        string StartMode);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
