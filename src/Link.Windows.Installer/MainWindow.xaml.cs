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
        _viewModel = new MainWindowViewModel(
            new WindowsSystemProbe(),
            new SystemCompatibilityEvaluator(),
            new SetupLogger());
        DataContext = _viewModel;
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _viewModel.Back();
    private void Next_Click(object sender, RoutedEventArgs e) => _viewModel.Next();

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
            "Cancel The Link setup?",
            "The Link Windows Setup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Close();
        }
    }

    private void RefreshSystemChecks_Click(object sender, RoutedEventArgs e) =>
        _viewModel.RefreshSystemChecks();

    private void Profile_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string profile })
            _viewModel.SelectProfile(profile);
    }
}
