param(
  [Parameter(Mandatory=$true)][string]$Source,
  [string]$Destination = (Join-Path (Split-Path -Parent $PSScriptRoot) "staging\link-core")
)

$ErrorActionPreference = "Stop"
$sourcePath = (Resolve-Path $Source).Path
$destinationPath = [System.IO.Path]::GetFullPath($Destination)

if (-not (Test-Path (Join-Path $sourcePath "release-manifest.json"))) {
  throw "release-manifest.json is required in the prepared Link-Core release folder."
}

if (Test-Path $destinationPath) {
  Remove-Item $destinationPath -Recurse -Force
}

New-Item $destinationPath -ItemType Directory -Force | Out-Null
Copy-Item (Join-Path $sourcePath "*") $destinationPath -Recurse -Force

Write-Host "Link-Core release staged at $destinationPath" -ForegroundColor Green
Write-Host "Run .\scripts\verify-release.ps1 before manual installer packaging."
