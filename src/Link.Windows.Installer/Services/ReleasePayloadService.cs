using System.Security.Cryptography;
using System.Text.Json;
using Link.Windows.Installer.Core.Models;
using Link.Windows.Installer.Core.Services;

namespace Link.Windows.Installer.Services;

public sealed class ReleasePayloadService
{
    private readonly ReleaseManifestValidator _validator;

    public ReleasePayloadService(ReleaseManifestValidator validator)
    {
        _validator = validator;
    }

    public string DefaultStagingDirectory =>
        Path.Combine(AppContext.BaseDirectory, "staging", "link-core");

    public string ManifestPath(string stagingDirectory) =>
        Path.Combine(stagingDirectory, "release-manifest.json");

    public ReleaseManifest LoadAndValidate(string stagingDirectory)
    {
        var manifestPath = ManifestPath(stagingDirectory);
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException(
                "Prepared Link-Core release manifest was not found. Stage the Windows release before final packaging.",
                manifestPath);

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ReleaseManifest>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Release manifest could not be parsed.");

        var errors = _validator.Validate(manifest);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        foreach (var file in manifest.Files.Where(file => file.Required))
        {
            var source = ResolveSafePath(stagingDirectory, file.Path);
            if (!File.Exists(source))
                throw new FileNotFoundException($"Required release file is missing: {file.Path}", source);

            var info = new FileInfo(source);
            if (info.Length != file.Size)
                throw new InvalidOperationException(
                    $"Release file size mismatch for {file.Path}. Expected {file.Size}, found {info.Length}.");

            using var stream = File.OpenRead(source);
            var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"SHA-256 mismatch for staged release file: {file.Path}");
        }

        return manifest;
    }

    public string ComputeManifestFingerprint(string stagingDirectory)
    {
        var path = ManifestPath(stagingDirectory);
        var json = File.ReadAllText(path);
        return _validator.ComputeManifestFingerprint(json);
    }

    public static string ResolveSafePath(string root, string relative)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(root, relative));
        if (!candidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Release path escapes staging root: {relative}");
        return candidate;
    }
}
