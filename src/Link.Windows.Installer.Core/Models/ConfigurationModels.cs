namespace Link.Windows.Installer.Core.Models;

public sealed record RuntimeConfiguration(
    string Environment,
    string UpdateChannel,
    string BackendUrl,
    string DatabaseMode,
    string DatabaseCredentialReference,
    string DataDirectory,
    string CacheDirectory,
    string LogDirectory,
    bool DiagnosticsEnabled,
    bool CrashReportingEnabled,
    string GeneratedBy,
    string GeneratedAtUtc);
