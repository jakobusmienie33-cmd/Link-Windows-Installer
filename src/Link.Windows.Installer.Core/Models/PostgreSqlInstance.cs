namespace Link.Windows.Installer.Core.Models;

public sealed record PostgreSqlInstance(
    string Id,
    string Version,
    string BaseDirectory,
    string DataDirectory,
    string ServiceName,
    int Port,
    bool IsRunning,
    string Source)
{
    public string DisplayName =>
        $"PostgreSQL {Version} • port {Port} • {(IsRunning ? "Running" : "Stopped/unknown")}";
}
