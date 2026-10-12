using Link.Windows.Installer.Core.Models;
using Link.Windows.Installer.Core.Services;

namespace Link.Windows.Installer.Services;

public sealed class InstallationEngine
{
    private readonly ReleasePayloadService _payload;
    private readonly InstallationStateStore _stateStore;
    private readonly DeploymentPlanner _deploymentPlanner;
    private readonly TransactionalDeploymentService _deployment;
    private readonly RuntimeConfigurationService _configuration;
    private readonly WindowsIntegrationService _windows;
    private readonly WindowsServiceRegistrationService _services;
    private readonly SetupLogger _logger;

    public InstallationEngine(
        ReleasePayloadService payload,
        InstallationStateStore stateStore,
        DeploymentPlanner deploymentPlanner,
        TransactionalDeploymentService deployment,
        RuntimeConfigurationService configuration,
        WindowsIntegrationService windows,
        WindowsServiceRegistrationService services,
        SetupLogger logger)
    {
        _payload = payload;
        _stateStore = stateStore;
        _deploymentPlanner = deploymentPlanner;
        _deployment = deployment;
        _configuration = configuration;
        _windows = windows;
        _services = services;
        _logger = logger;
    }

    public string StagingDirectory { get; set; } = string.Empty;

    public (ReleaseManifest Release, InstalledState? Installed, DeploymentPlan Plan) Prepare(
        InstallationLayout layout)
    {
        var staging = ResolveStagingDirectory();
        var release = _payload.LoadAndValidate(staging);
        var installed = _stateStore.TryLoad(layout.DataDirectory);
        var plan = _deploymentPlanner.Plan(
            release,
            installed,
            layout.ApplicationDirectory,
            layout.BackupDirectory);

        return (release, installed, plan);
    }

    public async Task<InstalledState> InstallAsync(
        InstallationLayout layout,
        RuntimeConfiguration configuration,
        bool createDesktopShortcut,
        bool createStartMenuShortcut,
        bool runAtStartup,
        bool enableDeviceBridge,
        bool enableSyncService,
        bool enableUpdater,
        bool enableDiagnostics,
        IProgress<DeploymentProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var staging = ResolveStagingDirectory();
        var release = _payload.LoadAndValidate(staging);
        var fingerprint = _payload.ComputeManifestFingerprint(staging);
        var installed = _stateStore.TryLoad(layout.DataDirectory);
        var plan = _deploymentPlanner.Plan(
            release,
            installed,
            layout.ApplicationDirectory,
            layout.BackupDirectory);

        // Never claim an enabled update/sync/device service was installed
        // when its binary was omitted from the signed, staged release.
        // Check BEFORE app deployment so missing optional-service payloads
        // cannot strand a partial installation.
        ValidateSelectedServicePayloads(
            staging, release, enableDeviceBridge, enableSyncService,
            enableUpdater, enableDiagnostics);

        _logger.Log("install", plan.Summary);
        await _deployment.DeployAsync(staging, release, plan, progress, cancellationToken);

        progress?.Report(new(91, "Writing configuration", "Saving client-safe runtime configuration."));
        var configPath = _configuration.Write(layout, configuration);
        _logger.Log("config", $"Runtime configuration written: {configPath}");

        var entryPoint = ReleasePayloadService.ResolveSafePath(
            layout.ApplicationDirectory,
            release.EntryPoint);

        progress?.Report(new(94, "Integrating with Windows", "Registering shortcuts, services and uninstall metadata."));
        RegisterShellIntegration(
            layout,
            entryPoint,
            release.Version,
            createDesktopShortcut,
            createStartMenuShortcut,
            runAtStartup);

        _services.RegisterSelectedServices(
            layout.ApplicationDirectory,
            enableDeviceBridge,
            enableSyncService,
            enableUpdater,
            enableDiagnostics);

        var state = new InstalledState(
            "The Link",
            release.Version,
            release.Architecture,
            release.EntryPoint,
            layout.ApplicationDirectory,
            layout.DataDirectory,
            DateTimeOffset.UtcNow.ToString("O"),
            fingerprint,
            configuration.UpdateChannel,
            layout.PerMachine,
            release.Files.Where(file => file.Required)
                .Select(file => file.Path).ToArray());

        _stateStore.Save(layout.DataDirectory, state);

        progress?.Report(new(98, "Running post-install validation", "Checking entry point, configuration and installer state."));
        ValidateInstalledState(layout, state);

        progress?.Report(new(100, "Installation complete", $"The Link {release.Version} is ready."));
        _logger.Log("install", $"Installation completed successfully: {release.Version}.");
        return state;
    }

    private static void ValidateSelectedServicePayloads(
        string staging, ReleaseManifest release,
        bool deviceBridge, bool sync, bool updater, bool diagnostics)
    {
        var selected = new (bool Enabled, string RelativePath)[]
        {
            (deviceBridge, "services/device-bridge/Link.DeviceBridge.exe"),
            (sync, "services/sync/Link.SyncService.exe"),
            (updater, "services/updater/Link.Updater.exe"),
            (diagnostics, "services/diagnostics/Link.Diagnostics.exe"),
        };

        foreach (var service in selected.Where(x => x.Enabled))
        {
            if (!release.Files.Any(x => x.Required &&
                string.Equals(
                    x.Path.Replace('\\', '/'),
                    service.RelativePath, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    $"Selected service is missing from the required release manifest: {service.RelativePath}");

            var source = ReleasePayloadService.ResolveSafePath(staging, service.RelativePath);
            if (!File.Exists(source))
                throw new FileNotFoundException(
                    $"Selected service executable is absent from staging: {service.RelativePath}", source);
        }
    }

    public void ValidateInstalledState(InstallationLayout layout, InstalledState state)
    {
        var entryPoint = ReleasePayloadService.ResolveSafePath(
            state.ApplicationDirectory,
            state.EntryPoint);

        if (!File.Exists(entryPoint))
            throw new InvalidOperationException("Post-install validation failed: application entry point is missing.");

        var config = Path.Combine(layout.DataDirectory, "Config", "runtime.json");
        if (!File.Exists(config))
            throw new InvalidOperationException("Post-install validation failed: runtime configuration is missing.");

        var saved = _stateStore.TryLoad(layout.DataDirectory);
        if (saved is null || !string.Equals(saved.Version, state.Version, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Post-install validation failed: installation state was not persisted.");
    }

    private void RegisterShellIntegration(
        InstallationLayout layout,
        string entryPoint,
        string version,
        bool desktop,
        bool startMenu,
        bool runAtStartup)
    {
        if (startMenu)
        {
            var startMenuRoot = layout.PerMachine
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
                : Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var shortcut = Path.Combine(startMenuRoot, "Programs", layout.StartMenuFolder, "The Link.lnk");
            _windows.CreateShortcut(shortcut, entryPoint, layout.ApplicationDirectory);
        }

        if (desktop)
        {
            var desktopRoot = layout.PerMachine
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            _windows.CreateShortcut(Path.Combine(desktopRoot, "The Link.lnk"), entryPoint, layout.ApplicationDirectory);
        }

        _windows.SetRunAtStartup(entryPoint, runAtStartup);

        var installerExe = Environment.ProcessPath ?? "Link.Windows.Installer.exe";
        _windows.RegisterUninstallMetadata(
            layout.ApplicationDirectory,
            version,
            $"\"{installerExe}\" --uninstall",
            layout.PerMachine);
    }

    private string ResolveStagingDirectory() =>
        string.IsNullOrWhiteSpace(StagingDirectory)
            ? _payload.DefaultStagingDirectory
            : StagingDirectory;
}
