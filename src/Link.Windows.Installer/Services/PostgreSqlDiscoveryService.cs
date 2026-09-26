using System.Diagnostics;
using System.Text.RegularExpressions;
using Link.Windows.Installer.Core.Models;
using Microsoft.Win32;

namespace Link.Windows.Installer.Services;

public sealed class PostgreSqlDiscoveryService
{
    public IReadOnlyList<PostgreSqlInstance> Discover()
    {
        if (!OperatingSystem.IsWindows())
            return Array.Empty<PostgreSqlInstance>();

        var found = new Dictionary<string, PostgreSqlInstance>(StringComparer.OrdinalIgnoreCase);

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            DiscoverRegistryInstallations(view, found);

        DiscoverProgramFiles(found);

        return found.Values
            .OrderByDescending(instance => ParseMajorVersion(instance.Version))
            .ThenBy(instance => instance.Port)
            .ToArray();
    }

    public string? FindPsqlExecutable(PostgreSqlInstance? instance)
    {
        if (instance is not null)
        {
            var candidate = Path.Combine(instance.BaseDirectory, "bin", "psql.exe");
            if (File.Exists(candidate))
                return candidate;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(folder.Trim(), "psql.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
                // Ignore malformed PATH entries.
            }
        }

        return null;
    }

    private static void DiscoverRegistryInstallations(
        RegistryView view,
        IDictionary<string, PostgreSqlInstance> found)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var installations = baseKey.OpenSubKey(@"SOFTWARE\PostgreSQL\Installations");
            if (installations is null) return;

            foreach (var installationName in installations.GetSubKeyNames())
            {
                using var installation = installations.OpenSubKey(installationName);
                if (installation is null) continue;

                var version = installation.GetValue("Version")?.ToString() ?? "Unknown";
                var baseDirectory = installation.GetValue("Base Directory")?.ToString() ?? string.Empty;
                var dataDirectory = installation.GetValue("Data Directory")?.ToString() ?? string.Empty;
                var serviceId = installation.GetValue("Service ID")?.ToString() ?? string.Empty;
                var port = ReadPort(dataDirectory);
                var id = string.IsNullOrWhiteSpace(installationName)
                    ? $"{version}:{baseDirectory}"
                    : installationName;

                found[id] = new PostgreSqlInstance(
                    id,
                    version,
                    baseDirectory,
                    dataDirectory,
                    serviceId,
                    port,
                    IsServiceRunning(serviceId),
                    $"Registry ({view})");
            }
        }
        catch
        {
            // PostgreSQL is optional for normal workstations.
        }
    }

    private static void DiscoverProgramFiles(IDictionary<string, PostgreSqlInstance> found)
    {
        try
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "PostgreSQL");

            if (!Directory.Exists(root)) return;

            foreach (var directory in Directory.GetDirectories(root))
            {
                var version = Path.GetFileName(directory);
                var id = $"folder:{directory}";
                if (found.Values.Any(existing =>
                        string.Equals(existing.BaseDirectory, directory, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var dataDirectory = Path.Combine(directory, "data");
                found[id] = new PostgreSqlInstance(
                    id,
                    version,
                    directory,
                    Directory.Exists(dataDirectory) ? dataDirectory : string.Empty,
                    string.Empty,
                    ReadPort(dataDirectory),
                    false,
                    "Program Files scan");
            }
        }
        catch
        {
            // Best-effort fallback.
        }
    }

    private static int ReadPort(string dataDirectory)
    {
        try
        {
            var config = Path.Combine(dataDirectory, "postgresql.conf");
            if (!File.Exists(config)) return 5432;

            foreach (var line in File.ReadLines(config))
            {
                var match = Regex.Match(line, @"^\s*port\s*=\s*(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
                    return port;
            }
        }
        catch
        {
            // Default PostgreSQL port is used when configuration cannot be read.
        }

        return 5432;
    }

    private static bool IsServiceRunning(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return false;

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"query \"{serviceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process is null) return false;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2500);
            return output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static int ParseMajorVersion(string version)
    {
        var match = Regex.Match(version ?? string.Empty, @"\d+");
        return match.Success && int.TryParse(match.Value, out var major) ? major : 0;
    }
}
