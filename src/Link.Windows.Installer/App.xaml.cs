using System.Windows;
using Link.Windows.Installer.Core.Models;
using Link.Windows.Installer.Core.Services;
using Link.Windows.Installer.Services;

namespace Link.Windows.Installer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(arg => arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)))
        {
            RunUninstall(e.Args);
            Shutdown();
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private static void RunUninstall(string[] args)
    {
        var purgeData = args.Any(arg => arg.Equals("--purge-data", StringComparison.OrdinalIgnoreCase));
        var perMachineData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "The Link");
        var userData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "The Link");

        var stateStore = new InstallationStateStore();
        var state = stateStore.TryLoad(perMachineData) ?? stateStore.TryLoad(userData);

        if (state is null)
        {
            MessageBox.Show(
                "The Link installation state could not be found. No business data was removed.",
                "The Link Uninstall",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dataRoot = state.DataDirectory;
        var layout = new InstallationLayout(
            state.ApplicationDirectory,
            dataRoot,
            Path.Combine(dataRoot, "Data"),
            Path.Combine(dataRoot, "Logs", "Setup"),
            Path.Combine(dataRoot, "Backups"),
            state.PerMachine,
            "The Link");

        if (purgeData)
        {
            var confirmation = MessageBox.Show(
                "This will remove The Link application AND local business/cache data, logs and backups from this computer. Continue?",
                "Confirm destructive uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;
        }

        var options = new UninstallOptions(
            RemoveApplicationFiles: true,
            RemoveShortcuts: true,
            RemoveStartupRegistration: true,
            RemoveInstallerMetadata: true,
            PreserveBusinessData: !purgeData,
            PreserveLogs: !purgeData,
            PreserveBackups: !purgeData);

        var plan = new UninstallPlanner().Create(layout, options);
        var windows = new WindowsIntegrationService();

        RemoveKnownShortcuts(layout);
        new WindowsServiceRegistrationService().RemoveKnownServices();
        windows.SetRunAtStartup(Path.Combine(layout.ApplicationDirectory, state.EntryPoint), false);
        windows.RemoveUninstallMetadata(state.PerMachine);

        if (!purgeData)
            stateStore.Delete(layout.DataDirectory);

        new UninstallExecutionService().Execute(plan);

        MessageBox.Show(
            purgeData
                ? "The Link application and selected local data were removed."
                : "The Link application was removed. Local business/cache data, logs and backups were preserved.",
            "The Link Uninstall",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static void RemoveKnownShortcuts(InstallationLayout layout)
    {
        var startMenuRoot = layout.PerMachine
            ? Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
            : Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        var desktopRoot = layout.PerMachine
            ? Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
            : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        foreach (var path in new[]
                 {
                     Path.Combine(startMenuRoot, "Programs", layout.StartMenuFolder, "The Link.lnk"),
                     Path.Combine(desktopRoot, "The Link.lnk")
                 })
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                var parent = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent) &&
                    !Directory.EnumerateFileSystemEntries(parent).Any())
                    Directory.Delete(parent);
            }
            catch
            {
                // Uninstall continues; stale shortcut can be removed manually.
            }
        }
    }
}
