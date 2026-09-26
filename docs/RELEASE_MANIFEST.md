# Link-Core Windows Release Manifest

The installer consumes a prepared Link-Core Windows release folder. It does not copy Link-Core source code.

## Required file

`release-manifest.json`

Example:

```json
{
  "product": "The Link",
  "version": "1.0.0",
  "architecture": "x64",
  "entryPoint": "the_link.exe",
  "channel": "Stable",
  "minimumInstallerVersion": "1.0.0",
  "files": [
    {
      "path": "the_link.exe",
      "sha256": "<64 hex characters>",
      "size": 12345678,
      "required": true
    }
  ]
}
```

Every required file is checked for a safe relative path, presence, exact byte size, and SHA-256 match. The manifest itself is fingerprinted into installation state.

## Create and stage a manifest

```powershell
.\scripts\new-release-manifest.ps1 -ReleaseFolder "C:\path\to\Link-Core\Release" -Version "1.0.0" -Architecture x64 -EntryPoint "the_link.exe"
.\scripts\stage-link-core.ps1 -Source "C:\path\to\Link-Core\Release"
.\scripts\verify-release.ps1
```
