using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Link.Windows.Installer.Core.Models;
using Link.Windows.Installer.Core.Services;
using Link.Windows.Installer.Models;
using Link.Windows.Installer.Services;

namespace Link.Windows.Installer.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly WindowsSystemProbe _probe;
    private readonly SystemCompatibilityEvaluator _systemEvaluator;
    private readonly InstallationPathService _pathService;
    private readonly PrerequisiteProbe _prerequisiteProbe;
    private readonly PrerequisiteEvaluator _prerequisiteEvaluator;
    private readonly PostgreSqlDiscoveryService _postgreSqlDiscovery;
    private readonly DatabaseSetupPlanner _databasePlanner;
    private readonly DatabaseBootstrapPlanner _bootstrapPlanner;
    private readonly DatabaseConnectivityService _connectivity;
    private readonly WindowsCredentialStore _credentialStore;
    private readonly InstallationEngine _installationEngine;
    private readonly SetupLogger _logger;

    private int _currentStepIndex;
    private string _selectedInstallProfile = "Standard Workstation";
    private string _systemCheckSummary = "Checking this computer…";
    private bool _hasBlockingChecks;
    private string _prerequisiteSummary = "Prerequisites have not been scanned yet.";
    private bool _hasBlockingPrerequisites;
    private PostgreSqlInstance? _selectedPostgreSql;
    private string _postgreSqlSummary = "PostgreSQL discovery has not run yet.";

    private DatabaseMode _selectedDatabaseMode = DatabaseMode.CloudSupabaseWithLocalSqlite;
    private string _backendUrl = string.Empty;
    private string _databaseHost = "localhost";
    private string _databasePort = "5432";
    private string _databaseName = "the_link";
    private string _databaseUser = "link_runtime";
    private string _databasePlanTitle = "Cloud Supabase + Local SQLite";
    private string _databasePlanSummary = "Enter the configured backend URL to complete the workstation database plan.";
    private string _databaseValidationSummary = "Connection validation has not run.";
    private DatabaseValidationState _databaseValidationState = DatabaseValidationState.Pending;
    private string _credentialReference = "No database secret stored.";

    private string _applicationDirectory = string.Empty;
    private string _dataDirectory = string.Empty;
    private string _cacheDirectory = string.Empty;
    private string _logDirectory = string.Empty;
    private string _backupDirectory = string.Empty;
    private bool _installForAllUsers = true;
    private string _startMenuFolder = "The Link";
    private string _pathValidationSummary = "Installation paths have not been validated.";
    private bool _hasPathErrors;

    private string _deviceDisplayName = Environment.MachineName;
    private string _workstationPurpose = "Resolve after sign-in";
    private bool _enableDeviceBridge = true;
    private bool _enableSyncService = true;
    private bool _enableUpdater = true;
    private bool _enableDiagnostics;

    private bool _createStartMenuShortcut = true;
    private bool _createDesktopShortcut = true;
    private bool _runAtStartup;
    private string _updateChannel = "Stable";
    private bool _diagnosticsEnabled;
    private bool _crashReportingEnabled = true;

    private bool _releaseReady;
    private string _releaseSummary = "Release payload has not been validated.";
    private string _deploymentMode = "Pending";
    private bool _isInstalling;
    private int _installProgress;
    private string _installStatus = "Waiting to start…";
    private string _completeSummary = "Installation has not run yet.";

    public MainWindowViewModel(
        WindowsSystemProbe probe,
        SystemCompatibilityEvaluator systemEvaluator,
        InstallationPathService pathService,
        PrerequisiteProbe prerequisiteProbe,
        PrerequisiteEvaluator prerequisiteEvaluator,
        PostgreSqlDiscoveryService postgreSqlDiscovery,
        DatabaseSetupPlanner databasePlanner,
        DatabaseBootstrapPlanner bootstrapPlanner,
        DatabaseConnectivityService connectivity,
        WindowsCredentialStore credentialStore,
        InstallationEngine installationEngine,
        SetupLogger logger)
    {
        _probe = probe;
        _systemEvaluator = systemEvaluator;
        _pathService = pathService;
        _prerequisiteProbe = prerequisiteProbe;
        _prerequisiteEvaluator = prerequisiteEvaluator;
        _postgreSqlDiscovery = postgreSqlDiscovery;
        _databasePlanner = databasePlanner;
        _bootstrapPlanner = bootstrapPlanner;
        _connectivity = connectivity;
        _credentialStore = credentialStore;
        _installationEngine = installationEngine;
        _logger = logger;

        Steps = new ObservableCollection<InstallerStep>(
            new[]
            {
                "Welcome",
                "System Check",
                "Install Type",
                "Components",
                "Database / SQL",
                "Install Location",
                "Device & Services",
                "Network & Security",
                "Updates & Privacy",
                "Review",
                "Installing",
                "Complete"
            }.Select((title, index) => new InstallerStep(index + 1, title)));

        ApplyLayout(_pathService.CreateDefaultLayout());
        RefreshDatabasePlan();
        UpdateStepStates();
    }

    public ObservableCollection<InstallerStep> Steps { get; }
    public ObservableCollection<SystemCheckResult> SystemChecks { get; } = new();
    public ObservableCollection<PrerequisiteCheck> Prerequisites { get; } = new();
    public ObservableCollection<PostgreSqlInstance> PostgreSqlInstances { get; } = new();
    public ObservableCollection<string> DatabasePlanSteps { get; } = new();
    public ObservableCollection<string> DatabaseSecurityControls { get; } = new();
    public ObservableCollection<string> PathMessages { get; } = new();
    public ObservableCollection<string> ReviewItems { get; } = new();
    public ObservableCollection<string> InstallLog { get; } = new();

    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        private set
        {
            if (_currentStepIndex == value) return;
            _currentStepIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FooterStatus));
            OnPropertyChanged(nameof(CanBack));
            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(NextButtonText));
            UpdateStepStates();
        }
    }

    public string SelectedInstallProfile
    {
        get => _selectedInstallProfile;
        private set => SetField(ref _selectedInstallProfile, value);
    }

    public string SystemCheckSummary { get => _systemCheckSummary; private set => SetField(ref _systemCheckSummary, value); }

    public bool HasBlockingChecks
    {
        get => _hasBlockingChecks;
        private set
        {
            if (SetField(ref _hasBlockingChecks, value))
                OnPropertyChanged(nameof(CanNext));
        }
    }

    public string PrerequisiteSummary { get => _prerequisiteSummary; private set => SetField(ref _prerequisiteSummary, value); }
    public bool HasBlockingPrerequisites { get => _hasBlockingPrerequisites; private set => SetField(ref _hasBlockingPrerequisites, value); }

    public PostgreSqlInstance? SelectedPostgreSql
    {
        get => _selectedPostgreSql;
        set
        {
            if (!SetField(ref _selectedPostgreSql, value) || value is null) return;
            DatabaseHost = "localhost";
            DatabasePort = value.Port.ToString();
            RefreshDatabasePlan();
        }
    }

    public string PostgreSqlSummary { get => _postgreSqlSummary; private set => SetField(ref _postgreSqlSummary, value); }

    public DatabaseMode SelectedDatabaseMode
    {
        get => _selectedDatabaseMode;
        private set
        {
            if (!SetField(ref _selectedDatabaseMode, value)) return;
            OnPropertyChanged(nameof(DatabaseModeLabel));
            RefreshDatabasePlan();
        }
    }

    public string DatabaseModeLabel => SelectedDatabaseMode switch
    {
        DatabaseMode.CloudSupabaseWithLocalSqlite => "Cloud Supabase + Local SQLite",
        DatabaseMode.ExistingPostgreSql => "Connect to Existing PostgreSQL",
        DatabaseMode.InstallLocalPostgreSql => "Install Local PostgreSQL",
        DatabaseMode.SqliteOnly => "Local SQLite Only / Offline Preparation",
        _ => SelectedDatabaseMode.ToString()
    };

    public string BackendUrl { get => _backendUrl; set { if (SetField(ref _backendUrl, value)) RefreshDatabasePlan(); } }
    public string DatabaseHost { get => _databaseHost; set { if (SetField(ref _databaseHost, value)) RefreshDatabasePlan(); } }
    public string DatabasePort { get => _databasePort; set { if (SetField(ref _databasePort, value)) RefreshDatabasePlan(); } }
    public string DatabaseName { get => _databaseName; set { if (SetField(ref _databaseName, value)) RefreshDatabasePlan(); } }
    public string DatabaseUser { get => _databaseUser; set { if (SetField(ref _databaseUser, value)) RefreshDatabasePlan(); } }
    public string DatabasePlanTitle { get => _databasePlanTitle; private set => SetField(ref _databasePlanTitle, value); }
    public string DatabasePlanSummary { get => _databasePlanSummary; private set => SetField(ref _databasePlanSummary, value); }
    public string DatabaseValidationSummary { get => _databaseValidationSummary; private set => SetField(ref _databaseValidationSummary, value); }
    public DatabaseValidationState DatabaseValidationState { get => _databaseValidationState; private set => SetField(ref _databaseValidationState, value); }
    public string CredentialReference { get => _credentialReference; private set => SetField(ref _credentialReference, value); }

    public string ApplicationDirectory { get => _applicationDirectory; set => SetField(ref _applicationDirectory, value); }
    public string DataDirectory { get => _dataDirectory; set => SetField(ref _dataDirectory, value); }
    public string CacheDirectory { get => _cacheDirectory; set => SetField(ref _cacheDirectory, value); }
    public string LogDirectory { get => _logDirectory; set => SetField(ref _logDirectory, value); }
    public string BackupDirectory { get => _backupDirectory; set => SetField(ref _backupDirectory, value); }

    public bool InstallForAllUsers
    {
        get => _installForAllUsers;
        set
        {
            if (!SetField(ref _installForAllUsers, value)) return;
            ApplyLayout(_pathService.CreateDefaultLayout(value));
        }
    }

    public string StartMenuFolder { get => _startMenuFolder; set => SetField(ref _startMenuFolder, value); }
    public string PathValidationSummary { get => _pathValidationSummary; private set => SetField(ref _pathValidationSummary, value); }
    public bool HasPathErrors { get => _hasPathErrors; private set => SetField(ref _hasPathErrors, value); }

    public string DeviceDisplayName { get => _deviceDisplayName; set => SetField(ref _deviceDisplayName, value); }
    public string WorkstationPurpose { get => _workstationPurpose; set => SetField(ref _workstationPurpose, value); }
    public bool EnableDeviceBridge { get => _enableDeviceBridge; set => SetField(ref _enableDeviceBridge, value); }
    public bool EnableSyncService { get => _enableSyncService; set => SetField(ref _enableSyncService, value); }
    public bool EnableUpdater { get => _enableUpdater; set => SetField(ref _enableUpdater, value); }
    public bool EnableDiagnostics { get => _enableDiagnostics; set => SetField(ref _enableDiagnostics, value); }

    public bool CreateStartMenuShortcut { get => _createStartMenuShortcut; set => SetField(ref _createStartMenuShortcut, value); }
    public bool CreateDesktopShortcut { get => _createDesktopShortcut; set => SetField(ref _createDesktopShortcut, value); }
    public bool RunAtStartup { get => _runAtStartup; set => SetField(ref _runAtStartup, value); }
    public string UpdateChannel { get => _updateChannel; set => SetField(ref _updateChannel, value); }
    public bool DiagnosticsEnabled { get => _diagnosticsEnabled; set => SetField(ref _diagnosticsEnabled, value); }
    public bool CrashReportingEnabled { get => _crashReportingEnabled; set => SetField(ref _crashReportingEnabled, value); }

    public bool ReleaseReady
    {
        get => _releaseReady;
        private set
        {
            if (SetField(ref _releaseReady, value))
                OnPropertyChanged(nameof(CanNext));
        }
    }

    public string ReleaseSummary { get => _releaseSummary; private set => SetField(ref _releaseSummary, value); }
    public string DeploymentMode { get => _deploymentMode; private set => SetField(ref _deploymentMode, value); }

    public bool IsInstalling
    {
        get => _isInstalling;
        private set
        {
            if (!SetField(ref _isInstalling, value)) return;
            OnPropertyChanged(nameof(CanBack));
            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(NextButtonText));
        }
    }

    public int InstallProgress { get => _installProgress; private set => SetField(ref _installProgress, value); }
    public string InstallStatus { get => _installStatus; private set => SetField(ref _installStatus, value); }
    public string CompleteSummary { get => _completeSummary; private set => SetField(ref _completeSummary, value); }

    public string FooterStatus => $"Step {CurrentStepIndex + 1} of 12 • {Steps[CurrentStepIndex].Title}";
    public bool CanBack => !IsInstalling && CurrentStepIndex > 0 && CurrentStepIndex < 10;
    public bool CanNext =>
        !IsInstalling &&
        CurrentStepIndex < 10 &&
        !(CurrentStepIndex == 1 && HasBlockingChecks) &&
        !(CurrentStepIndex == 3 && HasBlockingPrerequisites) &&
        !(CurrentStepIndex == 5 && HasPathErrors) &&
        !(CurrentStepIndex == 9 && !ReleaseReady);

    public string NextButtonText => CurrentStepIndex == 9 ? "Install" : CurrentStepIndex == 10 ? "Installing…" : "Next";
    public string LogPath => _logger.Path;

    public void RefreshSystemChecks()
    {
        _logger.Log("system", "Running Windows compatibility checks.");
        SystemChecks.Clear();
        var results = _systemEvaluator.Evaluate(_probe.Capture());
        foreach (var result in results) SystemChecks.Add(result);

        HasBlockingChecks = results.Any(result => result.State == SystemCheckState.Block);
        SystemCheckSummary = HasBlockingChecks
            ? "This computer has one or more blocking compatibility issues that must be resolved before setup can continue."
            : "This computer is ready for a standard The Link workstation installation.";
    }

    public void RefreshPrerequisites()
    {
        Prerequisites.Clear();
        var checks = _prerequisiteProbe.Capture(SelectedInstallProfile);
        foreach (var check in checks) Prerequisites.Add(check);
        HasBlockingPrerequisites = _prerequisiteEvaluator.HasBlockingItems(checks);
        PrerequisiteSummary = _prerequisiteEvaluator.Summarise(checks);
    }

    public void DiscoverPostgreSql()
    {
        PostgreSqlInstances.Clear();
        var instances = _postgreSqlDiscovery.Discover();
        foreach (var instance in instances) PostgreSqlInstances.Add(instance);

        if (instances.Count > 0)
        {
            SelectedPostgreSql ??= instances[0];
            PostgreSqlSummary = $"{instances.Count} PostgreSQL installation(s) discovered.";
        }
        else
        {
            PostgreSqlSummary = "No local PostgreSQL installation detected. This is normal for standard workstations.";
        }
    }

    public void SelectDatabaseMode(string mode)
    {
        SelectedDatabaseMode = mode switch
        {
            "cloud" => DatabaseMode.CloudSupabaseWithLocalSqlite,
            "existingpg" => DatabaseMode.ExistingPostgreSql,
            "installpg" => DatabaseMode.InstallLocalPostgreSql,
            "sqliteonly" => DatabaseMode.SqliteOnly,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown database mode.")
        };
        _logger.Log("database", $"Database mode selected: {DatabaseModeLabel}.");
    }

    public async Task ValidateDatabaseAsync(string password, CancellationToken cancellationToken = default)
    {
        var options = BuildDatabaseOptions();
        var errors = _databasePlanner.Validate(options);
        if (errors.Count > 0)
        {
            DatabaseValidationState = DatabaseValidationState.Invalid;
            DatabaseValidationSummary = string.Join(" ", errors);
            return;
        }

        DatabaseValidationResult result = options.Mode switch
        {
            DatabaseMode.CloudSupabaseWithLocalSqlite =>
                await _connectivity.TestCloudEndpointAsync(options.BackendUrl, cancellationToken),
            DatabaseMode.ExistingPostgreSql =>
                await _connectivity.TestPostgreSqlAsync(
                    options.Host,
                    options.Port,
                    options.DatabaseName,
                    options.UserName,
                    password,
                    _postgreSqlDiscovery.FindPsqlExecutable(SelectedPostgreSql),
                    cancellationToken),
            DatabaseMode.InstallLocalPostgreSql => new(
                DatabaseValidationState.Pending,
                "Local PostgreSQL validation is staged.",
                "The signed PostgreSQL package is installed during deployment, then validated."),
            DatabaseMode.SqliteOnly => new(
                DatabaseValidationState.Valid,
                "SQLite-only preparation is valid.",
                "Remote activation is intentionally deferred."),
            _ => throw new InvalidOperationException("Unsupported database mode.")
        };

        DatabaseValidationState = result.State;
        DatabaseValidationSummary = $"{result.Summary} {result.Detail}".Trim();

        if (options.Mode == DatabaseMode.ExistingPostgreSql && result.IsSuccessful && !string.IsNullOrWhiteSpace(password))
        {
            var target = _credentialStore.BuildDatabaseTarget(options);
            _credentialStore.Write(target, options.UserName, password);
            CredentialReference = target;
        }
    }

    public void ValidateInstallationLayout()
    {
        var result = _pathService.Validate(BuildInstallationLayout(), _probe.Capture().IsAdministrator);
        PathMessages.Clear();
        foreach (var error in result.Errors) PathMessages.Add($"BLOCK: {error}");
        foreach (var warning in result.Warnings) PathMessages.Add($"NOTE: {warning}");
        HasPathErrors = !result.IsValid;
        PathValidationSummary = result.IsValid
            ? "Installation paths are valid."
            : "One or more installation path problems must be corrected.";
    }

    public void RefreshReview()
    {
        ReviewItems.Clear();
        ValidateInstallationLayout();

        try
        {
            var prepared = _installationEngine.Prepare(BuildInstallationLayout());
            ReleaseReady = true;
            DeploymentMode = prepared.Plan.Mode.ToString();
            ReleaseSummary = $"The Link {prepared.Release.Version} • {prepared.Release.Architecture} • {prepared.Plan.Mode}";
            ReviewItems.Add($"Product: The Link {prepared.Release.Version}");
            ReviewItems.Add($"Deployment: {prepared.Plan.Mode}");
            ReviewItems.Add($"Install profile: {SelectedInstallProfile}");
            ReviewItems.Add($"Database mode: {DatabaseModeLabel}");
            ReviewItems.Add($"Application: {ApplicationDirectory}");
            ReviewItems.Add($"Local data: {DataDirectory}");
            ReviewItems.Add($"Device: {DeviceDisplayName} / {WorkstationPurpose}");
            ReviewItems.Add($"Services: Device Bridge={EnableDeviceBridge}, Sync={EnableSyncService}, Updater={EnableUpdater}, Diagnostics={EnableDiagnostics}");
            ReviewItems.Add($"Shortcuts: Start Menu={CreateStartMenuShortcut}, Desktop={CreateDesktopShortcut}, Start with Windows={RunAtStartup}");
            ReviewItems.Add($"Update channel: {UpdateChannel}");
            ReviewItems.Add("Rollback: application checkpoint before upgrade/repair.");
            ReviewItems.Add("Data policy: mutable business data remains outside the application directory.");
        }
        catch (Exception ex)
        {
            ReleaseReady = false;
            DeploymentMode = "Unavailable";
            ReleaseSummary = ex.Message;
            ReviewItems.Add("Stage and verify a prepared Link-Core Windows release before installing.");
        }
    }

    public async Task NextAsync(CancellationToken cancellationToken = default)
    {
        if (!CanNext) return;

        if (CurrentStepIndex == 0)
        {
            CurrentStepIndex = 1;
            RefreshSystemChecks();
            return;
        }

        if (CurrentStepIndex == 9)
        {
            CurrentStepIndex = 10;
            await RunInstallationAsync(cancellationToken);
            return;
        }

        CurrentStepIndex++;

        if (CurrentStepIndex == 3) RefreshPrerequisites();
        else if (CurrentStepIndex == 4)
        {
            DiscoverPostgreSql();
            RefreshDatabasePlan();
        }
        else if (CurrentStepIndex == 5) ValidateInstallationLayout();
        else if (CurrentStepIndex == 9) RefreshReview();
    }

    public void Back()
    {
        if (CanBack) CurrentStepIndex--;
    }

    public void SelectProfile(string profile)
    {
        SelectedInstallProfile = profile;
        if (profile.Contains("Branch Server", StringComparison.OrdinalIgnoreCase) &&
            SelectedDatabaseMode == DatabaseMode.CloudSupabaseWithLocalSqlite)
            SelectDatabaseMode("installpg");
        if (CurrentStepIndex >= 3) RefreshPrerequisites();
    }

    private async Task RunInstallationAsync(CancellationToken cancellationToken)
    {
        IsInstalling = true;
        InstallProgress = 0;
        InstallLog.Clear();

        try
        {
            _pathService.PrepareWritableDataDirectories(BuildInstallationLayout());

            var configuration = new RuntimeConfiguration(
                "Production",
                UpdateChannel,
                BackendUrl.Trim(),
                DatabaseModeLabel,
                CredentialReference,
                DataDirectory,
                CacheDirectory,
                LogDirectory,
                DiagnosticsEnabled,
                CrashReportingEnabled,
                "Link Windows Installer by MeetWell Technologies",
                DateTimeOffset.UtcNow.ToString("O"));

            var progress = new Progress<DeploymentProgress>(item =>
            {
                InstallProgress = item.Percent;
                InstallStatus = $"{item.Stage} • {item.Detail}";
                InstallLog.Add($"[{item.Percent}%] {item.Stage}: {item.Detail}");
            });

            var state = await _installationEngine.InstallAsync(
                BuildInstallationLayout(),
                configuration,
                CreateDesktopShortcut,
                CreateStartMenuShortcut,
                RunAtStartup,
                EnableDeviceBridge,
                EnableSyncService,
                EnableUpdater,
                EnableDiagnostics,
                progress,
                cancellationToken);

            CompleteSummary = $"The Link {state.Version} installed successfully. Application files, runtime configuration, shortcuts and installer state passed post-install validation.";
            CurrentStepIndex = 11;
        }
        catch (Exception ex)
        {
            InstallStatus = "Installation failed; rollback was attempted.";
            InstallLog.Add("[error] " + ex.Message);
            _logger.Log("error", ex.Message);
            CurrentStepIndex = 9;
            RefreshReview();
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private void RefreshDatabasePlan()
    {
        var options = BuildDatabaseOptions();
        var errors = _databasePlanner.Validate(options);
        DatabasePlanSteps.Clear();
        DatabaseSecurityControls.Clear();

        if (errors.Count > 0)
        {
            DatabasePlanTitle = DatabaseModeLabel;
            DatabasePlanSummary = "Configuration incomplete: " + string.Join(" ", errors);
            foreach (var error in errors) DatabasePlanSteps.Add("Complete: " + error);
            return;
        }

        var plan = _databasePlanner.CreatePlan(options);
        var bootstrap = _bootstrapPlanner.Create(options);
        DatabasePlanTitle = plan.Title;
        DatabasePlanSummary = plan.Summary;

        foreach (var step in plan.Steps) DatabasePlanSteps.Add(step);
        foreach (var action in bootstrap.OrderedActions.Where(action => !DatabasePlanSteps.Contains(action)))
            DatabasePlanSteps.Add(action);
        foreach (var control in bootstrap.SecurityControls) DatabaseSecurityControls.Add(control);

        if (bootstrap.RunsProductionCloudDdl)
            throw new InvalidOperationException("Installer security invariant violated: production cloud DDL is forbidden.");
    }

    private DatabaseSetupOptions BuildDatabaseOptions()
    {
        _ = int.TryParse(DatabasePort, out var port);
        if (port == 0) port = 5432;

        return new DatabaseSetupOptions(
            SelectedDatabaseMode,
            BackendUrl.Trim(),
            DatabaseHost.Trim(),
            port,
            DatabaseName.Trim(),
            DatabaseUser.Trim(),
            CacheDirectory,
            SelectedPostgreSql?.Id ?? string.Empty,
            true);
    }

    private InstallationLayout BuildInstallationLayout() =>
        new(
            ApplicationDirectory,
            DataDirectory,
            CacheDirectory,
            LogDirectory,
            BackupDirectory,
            InstallForAllUsers,
            StartMenuFolder);

    private void ApplyLayout(InstallationLayout layout)
    {
        ApplicationDirectory = layout.ApplicationDirectory;
        DataDirectory = layout.DataDirectory;
        CacheDirectory = layout.CacheDirectory;
        LogDirectory = layout.LogDirectory;
        BackupDirectory = layout.BackupDirectory;
        StartMenuFolder = layout.StartMenuFolder;
        ValidateInstallationLayout();
    }

    private void UpdateStepStates()
    {
        for (var index = 0; index < Steps.Count; index++)
        {
            Steps[index].IsComplete = index < CurrentStepIndex;
            Steps[index].IsCurrent = index == CurrentStepIndex;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
