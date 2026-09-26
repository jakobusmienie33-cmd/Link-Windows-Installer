$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

& (Join-Path $PSScriptRoot "validate-source.ps1") -Build
& (Join-Path $PSScriptRoot "verify-release.ps1")

$required = @(
  "src\Link.Windows.Installer\bin\Debug\net8.0-windows\Link.Windows.Installer.exe",
  "staging\link-core\release-manifest.json"
)

foreach ($relative in $required) {
  if (-not (Test-Path (Join-Path $root $relative))) {
    throw "Manual packaging input missing: $relative"
  }
}

Write-Host ""
Write-Host "MANUAL PACKAGE READINESS: PASS" -ForegroundColor Green
Write-Host "You may now use your chosen Windows EXE packaging/signing tool."
Write-Host "Do not package unsigned or unverified Link-Core payloads for production."
