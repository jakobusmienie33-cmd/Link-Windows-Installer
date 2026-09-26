using System.Security.Cryptography;
using System.Text;
using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Core.Services;

public sealed class ReleaseManifestValidator
{
    public IReadOnlyList<string> Validate(ReleaseManifest manifest)
    {
        var errors = new List<string>();

        if (!string.Equals(manifest.Product, "The Link", StringComparison.OrdinalIgnoreCase))
            errors.Add("Release manifest product must be 'The Link'.");

        if (!Version.TryParse(NormaliseVersion(manifest.Version), out _))
            errors.Add("Release manifest version is invalid.");

        if (manifest.Architecture is not ("x64" or "arm64"))
            errors.Add("Release manifest architecture must be x64 or arm64.");

        if (string.IsNullOrWhiteSpace(manifest.EntryPoint))
            errors.Add("Release manifest entry point is required.");

        if (manifest.Files.Count == 0)
            errors.Add("Release manifest must contain at least one file.");

        if (manifest.Files.Select(file => file.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Files.Count)
            errors.Add("Release manifest contains duplicate file paths.");

        foreach (var file in manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(file.Path) || Path.IsPathRooted(file.Path) || file.Path.Contains("..", StringComparison.Ordinal))
                errors.Add($"Release file path is not safe: {file.Path}");

            if (file.Required && !IsSha256(file.Sha256))
                errors.Add($"Required release file has an invalid SHA-256 hash: {file.Path}");

            if (file.Size < 0)
                errors.Add($"Release file has an invalid size: {file.Path}");
        }

        if (!manifest.Files.Any(file => string.Equals(
                Normalise(file.Path),
                Normalise(manifest.EntryPoint),
                StringComparison.OrdinalIgnoreCase)))
            errors.Add("Release entry point is not present in the manifest file list.");

        return errors;
    }

    public string ComputeManifestFingerprint(string canonicalJson)
    {
        var bytes = Encoding.UTF8.GetBytes(canonicalJson);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static string Normalise(string path) => path.Replace('\\', '/').TrimStart('/');

    private static string NormaliseVersion(string version)
    {
        var dash = version.IndexOf('-');
        return dash > 0 ? version[..dash] : version;
    }
}
