namespace Link.Windows.Installer.Core.Models;

public sealed record LayoutValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public static LayoutValidationResult Success(IReadOnlyList<string>? warnings = null) =>
        new(true, Array.Empty<string>(), warnings ?? Array.Empty<string>());
}
