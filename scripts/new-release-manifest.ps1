param(
  [Parameter(Mandatory=$true)][string]$ReleaseFolder,
  [Parameter(Mandatory=$true)][string]$Version,
  [ValidateSet("x64","arm64")][string]$Architecture = "x64",
  [string]$EntryPoint = "the_link.exe",
  [string]$Channel = "Stable"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path $ReleaseFolder).Path
$files = Get-ChildItem $root -File -Recurse |
  Where-Object { $_.Name -ne "release-manifest.json" } |
  ForEach-Object {
    [pscustomobject]@{
      path = [System.IO.Path]::GetRelativePath($root, $_.FullName).Replace("\","/")
      sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
      size = $_.Length
      required = $true
    }
  }

$manifest = [ordered]@{
  product = "The Link"
  version = $Version
  architecture = $Architecture
  entryPoint = $EntryPoint.Replace("\","/")
  channel = $Channel
  minimumInstallerVersion = "1.0.0"
  files = @($files)
}

$path = Join-Path $root "release-manifest.json"
$manifest | ConvertTo-Json -Depth 8 | Set-Content $path -Encoding UTF8
Write-Host "Release manifest created: $path" -ForegroundColor Green
