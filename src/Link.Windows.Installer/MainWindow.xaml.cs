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

        var postgreSqlDiscovery = new PostgreSqlDiscoveryService();
        var layoutPolicy = new InstallationLayoutPolicy();

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
            new SetupLogger());

        DataContext = _viewModel;
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _viewModel.Back();

    private void Next_Click(object sender, RoutedEventArgs e) => _viewModel.Next();

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Cancel The Link setup?",
            "The Link Windows Setup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            Close();
    }

    private void RefreshSystemChecks_Click(object sender, RoutedEventArgs e) =>
        _viewModel.RefreshSystemChecks();

    private void RefreshPrerequisites_Click(object sender, RoutedEventArgs e) =>
        _viewModel.RefreshPrerequisites();

    private void DiscoverPostgreSql_Click(object sender, RoutedEventArgs e) =>
        _viewModel.DiscoverPostgreSql();

    private async void ValidateDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.ValidateDatabaseAsync(PostgresPasswordBox.Password);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Database validation",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ValidatePaths_Click(object sender, RoutedEventArgs e) =>
        _viewModel.ValidateInstallationLayout();

    private void Profile_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel &&
            sender is RadioButton { Tag: string profile })
            viewModel.SelectProfile(profile);
    }

    private void DatabaseMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel &&
            sender is RadioButton { Tag: string mode })
            viewModel.SelectDatabaseMode(mode);
    }
}
