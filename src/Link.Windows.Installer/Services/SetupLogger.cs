namespace Link.Windows.Installer.Services;

public sealed class SetupLogger
{
    private readonly object _gate = new();
    private readonly string _path;

    public SetupLogger()
    {
        _path = ResolveLogPath();
        Log("setup", "Installer source shell initialised.");
    }

    public string Path => _path;

    public void Log(string category, string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{category}] {message}{Environment.NewLine}";
        lock (_gate)
        {
            File.AppendAllText(_path, line);
        }
    }

    private static string ResolveLogPath()
    {
        var preferred = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "The Link", "Logs", "Setup");

        try
        {
            Directory.CreateDirectory(preferred);
            return System.IO.Path.Combine(preferred, $"setup-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        }
        catch
        {
            var fallback = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "The Link", "Logs", "Setup");
            Directory.CreateDirectory(fallback);
            return System.IO.Path.Combine(fallback, $"setup-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        }
    }
}
