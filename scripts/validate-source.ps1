param([switch]$Build)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$required = @(
  "README.md","CHANGELOG.md","VERSION","Link.Windows.Installer.sln",
  "src/Link.Windows.Installer.Core/Link.Windows.Installer.Core.csproj",
  "src/Link.Windows.Installer/Link.Windows.Installer.csproj",
  "src/Link.Windows.Installer/MainWindow.xaml",
  "src/Link.Windows.Installer/Services/WindowsSystemProbe.cs"
)
foreach ($relative in $required) {
  if (-not (Test-Path (Join-Path $root $relative))) { throw "Required source file missing: $relative" }
}
$legacy = Get-ChildItem $root -Recurse -File |
  Where-Object { $_.FullName -notmatch "\\(bin|obj|\.git)\\" } |
  Select-String -SimpleMatch "Built@Home" -ErrorAction SilentlyContinue
if ($legacy) { throw "Legacy installer attribution found. Use 'By MeetWell Technologies'." }
$branding = Get-ChildItem $root -Recurse -File |
  Where-Object { $_.FullName -notmatch "\\(bin|obj|\.git)\\" } |
  Select-String -SimpleMatch "By MeetWell Technologies" -ErrorAction SilentlyContinue
if (-not $branding) { throw "MeetWell Technologies installer attribution is missing." }
Write-Host "Source structure and branding checks passed." -ForegroundColor Green
if ($Build) {
  dotnet build (Join-Path $root "Link.Windows.Installer.sln") -c Debug
  if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}
