namespace Link.Windows.Installer.Core.Models;

public sealed record SystemCheckResult(
    string Name,
    string Detail,
    string Badge,
    SystemCheckState State,
    string Icon);
