using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Link.Windows.Installer.Core.Models;
using Microsoft.Win32;

namespace Link.Windows.Installer.Services;

public sealed class WindowsSystemProbe
{
    public SystemSnapshot Capture()
    {
        var isWindows = OperatingSystem.IsWindows();
        var build = isWindows ? Environment.OSVersion.Version.Build : 0;
        var architecture = RuntimeInformation.OSArchitecture.ToString();
        var postgres = isWindows
            ? DetectPostgreSql()
            : (false, "PostgreSQL detection is available on Windows only.");

        return new SystemSnapshot(
            isWindows,
            GetWindowsName(isWindows, build),
            build,
            architecture,
            GetFreeDiskBytes(),
            isWindows ? GetPhysicalMemoryBytes() : 0,
            NetworkInterface.GetIsNetworkAvailable(),
            isWindows && IsAdministrator(),
            postgres.Item1,
            postgres.Item2);
    }

    private static string GetWindowsName(bool isWindows, int build)
    {
        if (!isWindows) return RuntimeInformation.OSDescription;

        var edition = ReadRegistryString(
            RegistryHive.LocalMachine,
            RegistryView.Registry64,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
            "ProductName");

        if (!string.IsNullOrWhiteSpace(edition))
        {
            if (build >= 22000 && edition.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
                edition = edition.Replace("Windows 10", "Windows 11", StringComparison.OrdinalIgnoreCase);
            return edition;
        }

        return build >= 22000 ? "Windows 11" : "Windows 10";
    }

    private static long GetFreeDiskBytes()
    {
        try
        {
            var root = System.IO.Path.GetPathRoot(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            return string.IsNullOrWhiteSpace(root) ? 0 : new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static (bool Found, string Description) DetectPostgreSql()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var installations = baseKey.OpenSubKey(@"SOFTWARE\PostgreSQL\Installations");
                if (installations is null) continue;

                foreach (var installationName in installations.GetSubKeyNames())
                {
                    using var installation = installations.OpenSubKey(installationName);
                    var version = installation?.GetValue("Version")?.ToString();
                    var baseDirectory = installation?.GetValue("Base Directory")?.ToString();

                    return (true, string.IsNullOrWhiteSpace(baseDirectory)
                        ? $"PostgreSQL {version ?? "installation"} detected."
                        : $"PostgreSQL {version ?? "installation"} detected at {baseDirectory}.");
                }
            }
            catch
            {
                // Registry detection is best-effort; standard workstations may have no PostgreSQL.
            }
        }

        try
        {
            var root = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "PostgreSQL");

            if (Directory.Exists(root))
            {
                var versions = Directory.GetDirectories(root)
                    .Select(System.IO.Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .OrderByDescending(name => name)
                    .ToArray();

                if (versions.Length > 0)
                    return (true, $"PostgreSQL installation folder detected ({string.Join(", ", versions)}).");
            }
        }
        catch
        {
            // Absence or inaccessible install metadata is a valid standard-workstation state.
        }

        return (false, "No local PostgreSQL installation detected.");
    }

    private static string? ReadRegistryString(
        RegistryHive hive,
        RegistryView view,
        string keyPath,
        string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(keyPath);
            return key?.GetValue(valueName)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static long GetPhysicalMemoryBytes()
    {
        var status = new MemoryStatusEx();
        return GlobalMemoryStatusEx(status) ? checked((long)status.TotalPhysical) : 0;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
