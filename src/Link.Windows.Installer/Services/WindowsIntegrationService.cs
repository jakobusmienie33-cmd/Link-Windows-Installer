using Microsoft.Win32;

namespace Link.Windows.Installer.Services;

public sealed class WindowsIntegrationService
{
    public void RegisterUninstallMetadata(
        string applicationDirectory,
        string version,
        string uninstallCommand,
        bool perMachine)
    {
        if (!OperatingSystem.IsWindows()) return;

        var hive = perMachine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
        var view = RegistryView.Registry64;
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var key = baseKey.CreateSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\TheLink");

        key.SetValue("DisplayName", "The Link");
        key.SetValue("Publisher", "MeetWell Technologies");
        key.SetValue("DisplayVersion", version);
        key.SetValue("InstallLocation", applicationDirectory);
        key.SetValue("UninstallString", uninstallCommand);
        key.SetValue("DisplayIcon", Path.Combine(applicationDirectory, "the_link.exe"));
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
    }

    public void SetRunAtStartup(string executablePath, bool enabled)
    {
        if (!OperatingSystem.IsWindows()) return;

        using var key = Registry.CurrentUser.CreateSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

        if (enabled)
            key.SetValue("The Link", Quote(executablePath));
        else
            key.DeleteValue("The Link", false);
    }

    public void CreateShortcut(
        string shortcutPath,
        string targetPath,
        string workingDirectory)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

        var escapedShortcut = EscapePowerShell(shortcutPath);
        var escapedTarget = EscapePowerShell(targetPath);
        var escapedWorking = EscapePowerShell(workingDirectory);

        var script =
            "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('" + escapedShortcut + "');" +
            "$s.TargetPath='" + escapedTarget + "';" +
            "$s.WorkingDirectory='" + escapedWorking + "';" +
            "$s.Description='The Link';" +
            "$s.Save();";

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "" + script.Replace(""", "\"") + """,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        process?.WaitForExit(10000);
        if (!File.Exists(shortcutPath))
            throw new InvalidOperationException($"Shortcut could not be created: {shortcutPath}");
    }

    public void RemoveUninstallMetadata(bool perMachine)
    {
        if (!OperatingSystem.IsWindows()) return;

        var hive = perMachine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var parent = baseKey.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            writable: true);
        parent?.DeleteSubKeyTree("TheLink", false);
    }

    private static string Quote(string value) => $"\"{value}\"";
    private static string EscapePowerShell(string value) => value.Replace("'", "''");
}
