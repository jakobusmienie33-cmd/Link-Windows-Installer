using Link.Windows.Installer.Core.Models;
using Microsoft.Win32;

namespace Link.Windows.Installer.Services;

public sealed class PrerequisiteProbe
{
    private readonly PostgreSqlDiscoveryService _postgreSqlDiscovery;

    public PrerequisiteProbe(PostgreSqlDiscoveryService postgreSqlDiscovery)
    {
        _postgreSqlDiscovery = postgreSqlDiscovery;
    }

    public IReadOnlyList<PrerequisiteCheck> Capture(string installProfile)
    {
        var vcRuntime = FindVcRuntime();
        var webView = FindWebView2();
        var postgreSql = _postgreSqlDiscovery.Discover();
        var branchServer = installProfile.Contains("Branch Server", StringComparison.OrdinalIgnoreCase);

        return new[]
        {
            new PrerequisiteCheck(
                "link-core",
                "The Link Windows payload",
                "The signed Link-Core Windows release payload is staged and verified during the application-deployment phase.",
                "P8 payload",
                PrerequisiteState.Planned,
                true,
                "Stage signed Link-Core release artifact"),

            new PrerequisiteCheck(
                "sqlite",
                "SQLite local data layer",
                "SQLite is supplied as part of the approved Link application/runtime and does not require a separate database server.",
                "Bundled",
                PrerequisiteState.Bundled,
                true,
                "Use bundled component"),

            new PrerequisiteCheck(
                "vc-runtime-x64",
                "Microsoft Visual C++ runtime (x64)",
                vcRuntime.Found
                    ? "Compatible x64 Visual C++ runtime registration was found."
                    : "The runtime was not detected. The signed prerequisite payload must supply it before Link-Core is launched.",
                vcRuntime.Version,
                vcRuntime.Found ? PrerequisiteState.Installed : PrerequisiteState.Planned,
                true,
                vcRuntime.Found ? "Use installed runtime" : "Install from signed prerequisite payload"),

            new PrerequisiteCheck(
                "webview2",
                "Microsoft Edge WebView2 Runtime",
                webView.Found
                    ? "WebView2 runtime detected for modules that use embedded web surfaces."
                    : "WebView2 was not detected. It remains optional unless the selected signed component manifest declares it required.",
                webView.Version,
                webView.Found ? PrerequisiteState.Installed : PrerequisiteState.Optional,
                false,
                webView.Found ? "Use installed runtime" : "Install only when declared by component manifest"),

            new PrerequisiteCheck(
                "postgresql",
                "PostgreSQL",
                postgreSql.Count > 0
                    ? $"{postgreSql.Count} local PostgreSQL installation(s) discovered."
                    : branchServer
                        ? "No local PostgreSQL was found. Branch Server setup can install an approved signed PostgreSQL package in the database step."
                        : "No local PostgreSQL was found. This is normal for standard workstations.",
                postgreSql.FirstOrDefault()?.Version ?? "Not installed",
                postgreSql.Count > 0
                    ? PrerequisiteState.Installed
                    : branchServer
                        ? PrerequisiteState.Planned
                        : PrerequisiteState.Optional,
                false,
                branchServer ? "Use existing or install approved local package" : "Not required")
        };
    }

    private static (bool Found, string Version) FindVcRuntime()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
                if (Convert.ToInt32(key?.GetValue("Installed") ?? 0) == 1)
                    return (true, key?.GetValue("Version")?.ToString() ?? "14.x");
            }
            catch
            {
                // Best-effort inventory.
            }
        }

        return (false, "Not detected");
    }

    private static (bool Found, string Version) FindWebView2()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var clients = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\EdgeUpdate\Clients");
                if (clients is null) continue;

                foreach (var childName in clients.GetSubKeyNames())
                {
                    using var child = clients.OpenSubKey(childName);
                    var name = child?.GetValue("name")?.ToString() ??
                               child?.GetValue("pv")?.ToString() ??
                               string.Empty;
                    var display = child?.GetValue("name")?.ToString() ?? string.Empty;

                    if (!display.Contains("WebView2", StringComparison.OrdinalIgnoreCase))
                        continue;

                    return (true, child?.GetValue("pv")?.ToString() ?? name);
                }
            }
            catch
            {
                // Optional runtime discovery is best-effort.
            }
        }

        return (false, "Not detected");
    }
}
