namespace Link.Windows.Installer.Core.Models;

public enum PrerequisiteState
{
    Installed,
    Bundled,
    Planned,
    Missing,
    Optional,
    Unknown
}

public sealed record PrerequisiteCheck(
    string Id,
    string Name,
    string Detail,
    string Version,
    PrerequisiteState State,
    bool IsRequired,
    string Action)
{
    public bool IsBlocking => IsRequired && State == PrerequisiteState.Missing;
}
