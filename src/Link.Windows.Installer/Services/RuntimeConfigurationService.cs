using System.Text.Json;
using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Services;

public sealed class RuntimeConfigurationService
{
    public string Write(
        InstallationLayout layout,
        RuntimeConfiguration configuration)
    {
        var configDirectory = Path.Combine(layout.DataDirectory, "Config");
        Directory.CreateDirectory(configDirectory);

        var path = Path.Combine(configDirectory, "runtime.json");
        var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        if (json.Contains("service_role", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsafe privileged Supabase configuration detected.");

        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, true);
        return path;
    }
}
