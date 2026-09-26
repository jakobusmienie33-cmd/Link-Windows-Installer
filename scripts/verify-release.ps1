param(
  [string]$Staging = (Join-Path (Split-Path -Parent $PSScriptRoot) "staging\link-core")
)

$ErrorActionPreference = "Stop"
$manifestPath = Join-Path $Staging "release-manifest.json"
if (-not (Test-Path $manifestPath)) {
  throw "Missing staged release manifest: $manifestPath"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
if ($manifest.product -ne "The Link") { throw "Unexpected product in release manifest." }
if (-not $manifest.version) { throw "Release version is missing." }
if ($manifest.architecture -notin @("x64","arm64")) { throw "Unsupported release architecture." }
if (-not $manifest.entryPoint) { throw "Release entryPoint is missing." }
if (-not $manifest.files -or $manifest.files.Count -eq 0) { throw "Release file list is empty." }

foreach ($file in $manifest.files) {
  if (-not $file.required) { continue }
  $path = Join-Path $Staging $file.path
  if (-not (Test-Path $path)) { throw "Required staged file missing: $($file.path)" }

  $hash = (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant()
  if ($hash -ne $file.sha256.ToLowerInvariant()) {
    throw "SHA256 mismatch: $($file.path)"
  }

  $size = (Get-Item $path).Length
  if ($size -ne [int64]$file.size) {
    throw "File-size mismatch: $($file.path)"
  }
}

Write-Host "Staged Link-Core release verified: $($manifest.version) / $($manifest.architecture)" -ForegroundColor Green
