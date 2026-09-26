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
    private readonly SystemCompatibilityEvaluator _evaluator;
    private readonly SetupLogger _logger;
    private int _currentStepIndex;
    private string _selectedInstallProfile = "Standard Workstation";
    private string _systemCheckSummary = "Checking this computer…";
    private bool _hasBlockingChecks;

    public MainWindowViewModel(
        WindowsSystemProbe probe,
        SystemCompatibilityEvaluator evaluator,
        SetupLogger logger)
    {
        _probe = probe;
        _evaluator = evaluator;
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

        UpdateStepStates();
    }

    public ObservableCollection<InstallerStep> Steps { get; }

    public ObservableCollection<SystemCheckResult> SystemChecks { get; } = new();

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
        private set
        {
            if (_selectedInstallProfile == value) return;
            _selectedInstallProfile = value;
            OnPropertyChanged();
        }
    }

    public string SystemCheckSummary
    {
        get => _systemCheckSummary;
        private set
        {
            if (_systemCheckSummary == value) return;
            _systemCheckSummary = value;
            OnPropertyChanged();
        }
    }

    public bool HasBlockingChecks
    {
        get => _hasBlockingChecks;
        private set
        {
            if (_hasBlockingChecks == value) return;
            _hasBlockingChecks = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanNext));
        }
    }

    public string FooterStatus => $"Step {CurrentStepIndex + 1} of 12 • {Steps[CurrentStepIndex].Title}";

    public bool CanBack => CurrentStepIndex > 0 && CurrentStepIndex <= 3;

    public bool CanNext => CurrentStepIndex < 3 && !(CurrentStepIndex == 1 && HasBlockingChecks);

    public string LogPath => _logger.Path;

    public void RefreshSystemChecks()
    {
        _logger.Log("system", "Running Windows compatibility checks.");

        SystemChecks.Clear();
        var snapshot = _probe.Capture();
        var results = _evaluator.Evaluate(snapshot);

        foreach (var result in results)
            SystemChecks.Add(result);

        HasBlockingChecks = results.Any(result => result.State == SystemCheckState.Block);
        SystemCheckSummary = HasBlockingChecks
            ? "This computer has one or more blocking compatibility issues that must be resolved before setup can continue."
            : "This computer is ready for a standard The Link workstation installation.";

        _logger.Log(
            "system",
            HasBlockingChecks
                ? "Compatibility checks completed with blockers."
                : "Compatibility checks completed without blockers.");
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
    }

    public void Back()
    {
        if (CanBack) CurrentStepIndex--;
    }

    public void SelectProfile(string profile)
    {
        SelectedInstallProfile = profile;
        _logger.Log("profile", $"Install profile selected: {profile}.");
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
