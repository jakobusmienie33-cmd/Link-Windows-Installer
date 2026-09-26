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
            UpdateStepStates();
        }
    }

    public string SelectedInstallProfile
    {
        get => _selectedInstallProfile;
        private set => SetField(ref _selectedInstallProfile, value);
    }

    public string SystemCheckSummary
    {
        get => _systemCheckSummary;
        private set => SetField(ref _systemCheckSummary, value);
    }

    public bool HasBlockingChecks
    {
        get => _hasBlockingChecks;
        private set
        {
            if (SetField(ref _hasBlockingChecks, value))
                OnPropertyChanged(nameof(CanNext));
        }
    }

    public string PrerequisiteSummary
    {
        get => _prerequisiteSummary;
        private set => SetField(ref _prerequisiteSummary, value);
    }

    public bool HasBlockingPrerequisites
    {
        get => _hasBlockingPrerequisites;
        private set => SetField(ref _hasBlockingPrerequisites, value);
    }

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

    public string PostgreSqlSummary
    {
        get => _postgreSqlSummary;
        private set => SetField(ref _postgreSqlSummary, value);
    }

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

    public string BackendUrl
    {
        get => _backendUrl;
        set
        {
            if (SetField(ref _backendUrl, value))
                RefreshDatabasePlan();
        }
    }

    public string DatabaseHost
    {
        get => _databaseHost;
        set
        {
            if (SetField(ref _databaseHost, value))
                RefreshDatabasePlan();
        }
    }

    public string DatabasePort
    {
        get => _databasePort;
        set
        {
            if (SetField(ref _databasePort, value))
                RefreshDatabasePlan();
        }
    }

    public string DatabaseName
    {
        get => _databaseName;
        set
        {
            if (SetField(ref _databaseName, value))
                RefreshDatabasePlan();
        }
    }

    public string DatabaseUser
    {
        get => _databaseUser;
        set
        {
            if (SetField(ref _databaseUser, value))
                RefreshDatabasePlan();
        }
    }

    public string DatabasePlanTitle
    {
        get => _databasePlanTitle;
        private set => SetField(ref _databasePlanTitle, value);
    }

    public string DatabasePlanSummary
    {
        get => _databasePlanSummary;
        private set => SetField(ref _databasePlanSummary, value);
    }

    public string DatabaseValidationSummary
    {
        get => _databaseValidationSummary;
        private set => SetField(ref _databaseValidationSummary, value);
    }

    public DatabaseValidationState DatabaseValidationState
    {
        get => _databaseValidationState;
        private set => SetField(ref _databaseValidationState, value);
    }

    public string CredentialReference
    {
        get => _credentialReference;
        private set => SetField(ref _credentialReference, value);
    }

    public string ApplicationDirectory
    {
        get => _applicationDirectory;
        set => SetField(ref _applicationDirectory, value);
    }

    public string DataDirectory
    {
        get => _dataDirectory;
        set => SetField(ref _dataDirectory, value);
    }

    public string CacheDirectory
    {
        get => _cacheDirectory;
        set => SetField(ref _cacheDirectory, value);
    }

    public string LogDirectory
    {
        get => _logDirectory;
        set => SetField(ref _logDirectory, value);
    }

    public string BackupDirectory
    {
        get => _backupDirectory;
        set => SetField(ref _backupDirectory, value);
    }

    public bool InstallForAllUsers
    {
        get => _installForAllUsers;
        set
        {
            if (!SetField(ref _installForAllUsers, value)) return;
            ApplyLayout(_pathService.CreateDefaultLayout(value));
        }
    }

    public string StartMenuFolder
    {
        get => _startMenuFolder;
        set => SetField(ref _startMenuFolder, value);
    }

    public string PathValidationSummary
    {
        get => _pathValidationSummary;
        private set => SetField(ref _pathValidationSummary, value);
    }

    public bool HasPathErrors
    {
        get => _hasPathErrors;
        private set => SetField(ref _hasPathErrors, value);
    }

    public string FooterStatus => $"Step {CurrentStepIndex + 1} of 12 • {Steps[CurrentStepIndex].Title}";
    public bool CanBack => CurrentStepIndex > 0 && CurrentStepIndex <= 5;
    public bool CanNext => CurrentStepIndex < 5 && !(CurrentStepIndex == 1 && HasBlockingChecks);
    public string LogPath => _logger.Path;

    public void RefreshSystemChecks()
    {
        _logger.Log("system", "Running Windows compatibility checks.");
        SystemChecks.Clear();

        var snapshot = _probe.Capture();
        var results = _systemEvaluator.Evaluate(snapshot);
        foreach (var result in results)
            SystemChecks.Add(result);

        HasBlockingChecks = results.Any(result => result.State == SystemCheckState.Block);
        SystemCheckSummary = HasBlockingChecks
            ? "This computer has one or more blocking compatibility issues that must be resolved before setup can continue."
            : "This computer is ready for a standard The Link workstation installation.";

        _logger.Log("system", HasBlockingChecks
            ? "Compatibility checks completed with blockers."
            : "Compatibility checks completed without blockers.");
    }

    public void RefreshPrerequisites()
    {
        _logger.Log("prerequisite", $"Scanning prerequisites for {SelectedInstallProfile}.");
        Prerequisites.Clear();

        var checks = _prerequisiteProbe.Capture(SelectedInstallProfile);
        foreach (var check in checks)
            Prerequisites.Add(check);

        HasBlockingPrerequisites = _prerequisiteEvaluator.HasBlockingItems(checks);
        PrerequisiteSummary = _prerequisiteEvaluator.Summarise(checks);
    }

    public void DiscoverPostgreSql()
    {
        _logger.Log("database", "Discovering local PostgreSQL installations.");
        PostgreSqlInstances.Clear();

        var instances = _postgreSqlDiscovery.Discover();
        foreach (var instance in instances)
            PostgreSqlInstances.Add(instance);

        if (instances.Count > 0)
        {
            SelectedPostgreSql ??= instances[0];
            PostgreSqlSummary = $"{instances.Count} PostgreSQL installation(s) discovered. Select one or enter an approved server manually.";
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

        DatabaseValidationResult result;

        switch (options.Mode)
        {
            case DatabaseMode.CloudSupabaseWithLocalSqlite:
                result = await _connectivity.TestCloudEndpointAsync(options.BackendUrl, cancellationToken);
                break;

            case DatabaseMode.ExistingPostgreSql:
                result = await _connectivity.TestPostgreSqlAsync(
                    options.Host,
                    options.Port,
                    options.DatabaseName,
                    options.UserName,
                    password,
                    _postgreSqlDiscovery.FindPsqlExecutable(SelectedPostgreSql),
                    cancellationToken);
                break;

            case DatabaseMode.InstallLocalPostgreSql:
                result = new(
                    DatabaseValidationState.Pending,
                    "Local PostgreSQL validation is staged.",
                    "The signed PostgreSQL package is installed in the deployment phase; connectivity and restricted-role self-tests run immediately after installation.");
                break;

            case DatabaseMode.SqliteOnly:
                result = new(
                    DatabaseValidationState.Valid,
                    "SQLite-only preparation is valid.",
                    "Remote activation is intentionally deferred.");
                break;

            default:
                throw new InvalidOperationException("Unsupported database mode.");
        }

        DatabaseValidationState = result.State;
        DatabaseValidationSummary = $"{result.Summary} {result.Detail}".Trim();

        if (options.Mode == DatabaseMode.ExistingPostgreSql &&
            result.IsSuccessful &&
            !string.IsNullOrWhiteSpace(password))
        {
            var target = _credentialStore.BuildDatabaseTarget(options);
            _credentialStore.Write(target, options.UserName, password);
            CredentialReference = $"Stored securely in Windows Credential Manager: {target}";
            _logger.Log("security", "Restricted PostgreSQL runtime credential stored in Windows Credential Manager.");
        }

        _logger.Log("database", $"Database validation result: {result.State}. {result.Summary}");
    }

    public void ValidateInstallationLayout()
    {
        var layout = BuildInstallationLayout();
        var isAdministrator = _probe.Capture().IsAdministrator;
        var result = _pathService.Validate(layout, isAdministrator);

        PathMessages.Clear();
        foreach (var error in result.Errors)
            PathMessages.Add($"BLOCK: {error}");
        foreach (var warning in result.Warnings)
            PathMessages.Add($"NOTE: {warning}");

        HasPathErrors = !result.IsValid;
        PathValidationSummary = result.IsValid
            ? "Installation paths are valid. Writable data directories will be created only when installation begins."
            : "One or more installation path problems must be corrected.";

        _logger.Log("paths", result.IsValid ? "Installation layout validated." : "Installation layout validation failed.");
    }

    public IReadOnlyList<string> PrepareDataDirectoriesForInstall()
    {
        var layout = BuildInstallationLayout();
        var validation = _pathService.Validate(layout, _probe.Capture().IsAdministrator);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(" ", validation.Errors));

        return _pathService.PrepareWritableDataDirectories(layout);
    }

    public void Next()
    {
        if (!CanNext) return;

        if (CurrentStepIndex == 0)
        {
            CurrentStepIndex = 1;
            RefreshSystemChecks();
            return;
        }

        CurrentStepIndex++;

        if (CurrentStepIndex == 3)
            RefreshPrerequisites();
        else if (CurrentStepIndex == 4)
        {
            DiscoverPostgreSql();
            RefreshDatabasePlan();
        }
        else if (CurrentStepIndex == 5)
            ValidateInstallationLayout();
    }

    public void Back()
    {
        if (CanBack) CurrentStepIndex--;
    }

    public void SelectProfile(string profile)
    {
        SelectedInstallProfile = profile;
        _logger.Log("profile", $"Install profile selected: {profile}.");

        if (profile.Contains("Branch Server", StringComparison.OrdinalIgnoreCase) &&
            SelectedDatabaseMode == DatabaseMode.CloudSupabaseWithLocalSqlite)
            SelectDatabaseMode("installpg");

        if (CurrentStepIndex >= 3)
            RefreshPrerequisites();
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
            foreach (var error in errors)
                DatabasePlanSteps.Add("Complete: " + error);
            return;
        }

        var plan = _databasePlanner.CreatePlan(options);
        var bootstrap = _bootstrapPlanner.Create(options);

        DatabasePlanTitle = plan.Title;
        DatabasePlanSummary = plan.Summary;

        foreach (var step in plan.Steps)
            DatabasePlanSteps.Add(step);
        foreach (var action in bootstrap.OrderedActions.Where(action => !DatabasePlanSteps.Contains(action)))
            DatabasePlanSteps.Add(action);
        foreach (var control in bootstrap.SecurityControls)
            DatabaseSecurityControls.Add(control);

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
