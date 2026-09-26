using System.Text.Json;
using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Services;

public sealed class InstallationStateStore
{
    private const string StateFileName = "installation-state.json";

    public string GetStatePath(string dataDirectory) =>
        Path.Combine(dataDirectory, "Installer", StateFileName);

    public InstalledState? TryLoad(string dataDirectory)
    {
        try
        {
            var path = GetStatePath(dataDirectory);
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<InstalledState>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    public void Save(string dataDirectory, InstalledState state)
    {
        var path = GetStatePath(dataDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, true);
    }

    public void Delete(string dataDirectory)
    {
        var path = GetStatePath(dataDirectory);
        if (File.Exists(path))
            File.Delete(path);
    }
}
