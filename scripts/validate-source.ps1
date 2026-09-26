param([switch]$Build)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$required = @(
  "README.md",
  "CHANGELOG.md",
  "VERSION",
  "Link.Windows.Installer.sln",
  "docs/PHASES.md",
  "docs/DATABASE_SECURITY.md",
  "docs/INSTALLATION_LAYOUT.md",
  "docs/PREREQUISITES.md",
  "src/Link.Windows.Installer.Core/Link.Windows.Installer.Core.csproj",
  "src/Link.Windows.Installer.Core/Models/InstallationLayout.cs",
  "src/Link.Windows.Installer.Core/Models/PrerequisiteCheck.cs",
  "src/Link.Windows.Installer.Core/Models/PostgreSqlInstance.cs",
  "src/Link.Windows.Installer.Core/Models/DatabaseModels.cs",
  "src/Link.Windows.Installer.Core/Services/InstallationLayoutPolicy.cs",
  "src/Link.Windows.Installer.Core/Services/PrerequisiteEvaluator.cs",
  "src/Link.Windows.Installer.Core/Services/DatabaseSetupPlanner.cs",
  "src/Link.Windows.Installer.Core/Services/DatabaseBootstrapPlanner.cs",
  "src/Link.Windows.Installer/Link.Windows.Installer.csproj",
  "src/Link.Windows.Installer/MainWindow.xaml",
  "src/Link.Windows.Installer/Services/WindowsSystemProbe.cs",
  "src/Link.Windows.Installer/Services/InstallationPathService.cs",
  "src/Link.Windows.Installer/Services/PrerequisiteProbe.cs",
  "src/Link.Windows.Installer/Services/PostgreSqlDiscoveryService.cs",
  "src/Link.Windows.Installer/Services/DatabaseConnectivityService.cs",
  "src/Link.Windows.Installer/Services/WindowsCredentialStore.cs"
)

foreach ($relative in $required) {
  if (-not (Test-Path (Join-Path $root $relative))) {
    throw "Required source file missing: $relative"
  }
}

$sourceFiles = Get-ChildItem $root -Recurse -File |
  Where-Object {
    $_.FullName -notlike '*\bin\*' -and
    $_.FullName -notlike '*\obj\*' -and
    $_.FullName -notlike '*\.git\*'
  }

$legacyAttribution = "Built" + "@" + "Home"
$legacy = $sourceFiles |
  Where-Object { $_.FullName -ne $PSCommandPath } |
  Select-String -SimpleMatch $legacyAttribution -ErrorAction SilentlyContinue

if ($legacy) {
  throw "Legacy installer attribution found. Use 'By MeetWell Technologies'."
}

$branding = $sourceFiles |
  Select-String -SimpleMatch "By MeetWell Technologies" -ErrorAction SilentlyContinue

if (-not $branding) {
  throw "MeetWell Technologies installer attribution is missing."
}

$version = (Get-Content (Join-Path $root "VERSION") -Raw).Trim()
if ($version -ne "0.7.0-dev") {
  throw "Unexpected installer development version: $version"
}

$bootstrap = Get-Content (Join-Path $root "src/Link.Windows.Installer.Core/Services/DatabaseBootstrapPlanner.cs") -Raw
if ($bootstrap -notmatch "RunsProductionCloudDdl" -or $bootstrap -notmatch "false") {
  throw "Database bootstrap production-cloud-DDL invariant is missing."
}

Write-Host "Source structure, branding, P3-P7 contracts and version checks passed." -ForegroundColor Green

if ($Build) {
  dotnet build (Join-Path $root "Link.Windows.Installer.sln") -c Debug
  if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed."
  }

  dotnet run --project (Join-Path $root "tests/Link.Windows.Installer.Core.SmokeTests/Link.Windows.Installer.Core.SmokeTests.csproj") -c Debug --no-build
  if ($LASTEXITCODE -ne 0) {
    throw "Core smoke tests failed."
  }
}
