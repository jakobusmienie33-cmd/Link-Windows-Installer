using System.Windows;
using System.Windows.Controls;
using Link.Windows.Installer.Core.Services;
using Link.Windows.Installer.Services;
using Link.Windows.Installer.ViewModels;

namespace Link.Windows.Installer;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var logger = new SetupLogger();
        var postgreSqlDiscovery = new PostgreSqlDiscoveryService();
        var layoutPolicy = new InstallationLayoutPolicy();
        var releaseValidator = new ReleaseManifestValidator();
        var payload = new ReleasePayloadService(releaseValidator);

        var engine = new InstallationEngine(
            payload,
            new InstallationStateStore(),
            new DeploymentPlanner(),
            new TransactionalDeploymentService(),
            new RuntimeConfigurationService(),
            new WindowsIntegrationService(),
            new WindowsServiceRegistrationService(),
            logger);

        _viewModel = new MainWindowViewModel(
            new WindowsSystemProbe(),
            new SystemCompatibilityEvaluator(),
            new InstallationPathService(layoutPolicy),
            new PrerequisiteProbe(postgreSqlDiscovery),
            new PrerequisiteEvaluator(),
            postgreSqlDiscovery,
            new DatabaseSetupPlanner(),
            new DatabaseBootstrapPlanner(),
            new DatabaseConnectivityService(),
            new WindowsCredentialStore(),
            engine,
            logger);

        DataContext = _viewModel;
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _viewModel.Back();

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.NextAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "The Link Windows Setup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsInstalling)
            return;

        var result = MessageBox.Show(
            "Cancel The Link setup?",
            "The Link Windows Setup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            Close();
    }

    private void RefreshSystemChecks_Click(object sender, RoutedEventArgs e) => _viewModel.RefreshSystemChecks();
    private void RefreshPrerequisites_Click(object sender, RoutedEventArgs e) => _viewModel.RefreshPrerequisites();
    private void DiscoverPostgreSql_Click(object sender, RoutedEventArgs e) => _viewModel.DiscoverPostgreSql();

    private async void ValidateDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.ValidateDatabaseAsync(PostgresPasswordBox.Password);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Database validation", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ValidatePaths_Click(object sender, RoutedEventArgs e) => _viewModel.ValidateInstallationLayout();

    private void RefreshReview_Click(object sender, RoutedEventArgs e) => _viewModel.RefreshReview();

    private void Profile_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is RadioButton { Tag: string profile })
            viewModel.SelectProfile(profile);
    }

    private void DatabaseMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is RadioButton { Tag: string mode })
            viewModel.SelectDatabaseMode(mode);
    }
}
