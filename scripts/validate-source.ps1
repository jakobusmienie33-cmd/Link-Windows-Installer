param([switch]$Build)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$required = @(
  "README.md","CHANGELOG.md","VERSION","Link.Windows.Installer.sln",
  "docs/PHASES.md","docs/DATABASE_SECURITY.md","docs/INSTALLATION_LAYOUT.md",
  "docs/PREREQUISITES.md","docs/RELEASE_MANIFEST.md",
  "docs/UPGRADE_REPAIR_UNINSTALL.md","docs/MANUAL_PACKAGING.md",
  "scripts/new-release-manifest.ps1","scripts/stage-link-core.ps1",
  "scripts/verify-release.ps1","scripts/manual-package-check.ps1",
  "src/Link.Windows.Installer.Core/Models/DeploymentModels.cs",
  "src/Link.Windows.Installer.Core/Models/UninstallModels.cs",
  "src/Link.Windows.Installer.Core/Models/ConfigurationModels.cs",
  "src/Link.Windows.Installer.Core/Services/ReleaseManifestValidator.cs",
  "src/Link.Windows.Installer.Core/Services/DeploymentPlanner.cs",
  "src/Link.Windows.Installer.Core/Services/UninstallPlanner.cs",
  "src/Link.Windows.Installer/Services/ReleasePayloadService.cs",
  "src/Link.Windows.Installer/Services/InstallationStateStore.cs",
  "src/Link.Windows.Installer/Services/RuntimeConfigurationService.cs",
  "src/Link.Windows.Installer/Services/TransactionalDeploymentService.cs",
  "src/Link.Windows.Installer/Services/WindowsIntegrationService.cs",
  "src/Link.Windows.Installer/Services/WindowsServiceRegistrationService.cs",
  "src/Link.Windows.Installer/Services/UninstallExecutionService.cs",
  "src/Link.Windows.Installer/Services/InstallationEngine.cs",
  "src/Link.Windows.Installer/MainWindow.xaml",
  "src/Link.Windows.Installer/ViewModels/MainWindowViewModel.cs"
)

foreach ($relative in $required) {
  if (-not (Test-Path (Join-Path $root $relative))) {
    throw "Required installer source missing: $relative"
  }
}

$sourceFiles = Get-ChildItem $root -Recurse -File |
  Where-Object {
    $_.FullName -notlike '*\bin\*' -and
    $_.FullName -notlike '*\obj\*' -and
    $_.FullName -notlike '*\.git\*'
  }

$legacyAttribution = "Built" + "@" + "Home"
if ($sourceFiles | Where-Object { $_.FullName -ne $PSCommandPath } |
    Select-String -SimpleMatch $legacyAttribution -ErrorAction SilentlyContinue) {
  throw "Legacy installer attribution found. Use 'By MeetWell Technologies'."
}

if (-not ($sourceFiles | Select-String -SimpleMatch "By MeetWell Technologies" -ErrorAction SilentlyContinue)) {
  throw "MeetWell Technologies installer attribution is missing."
}

$version = (Get-Content (Join-Path $root "VERSION") -Raw).Trim()
if ($version -ne "1.0.0-rc1") {
  throw "Unexpected installer source version: $version"
}

$xaml = Get-Content (Join-Path $root "src/Link.Windows.Installer/MainWindow.xaml") -Raw
foreach ($requiredScreen in @(
  "Device &amp; Local Services",
  "Network, Firewall &amp; Security",
  "Updates, Privacy &amp; Diagnostics",
  "Ready to Install",
  "Installing The Link",
  "The Link is ready"
)) {
  if (-not $xaml.Contains($requiredScreen)) {
    throw "Final installer screen missing from canonical WPF shell: $requiredScreen"
  }
}

$bootstrap = Get-Content (Join-Path $root "src/Link.Windows.Installer.Core/Services/DatabaseBootstrapPlanner.cs") -Raw
if (-not $bootstrap.Contains("Production PostgreSQL RLS/policies/explicit Data API grants remain centrally managed")) {
  throw "Production cloud database boundary is missing."
}

$runtimeConfig = Get-Content (Join-Path $root "src/Link.Windows.Installer/Services/RuntimeConfigurationService.cs") -Raw
if (-not $runtimeConfig.Contains("service_role")) {
  throw "Runtime privileged-secret guard is missing."
}

$uninstall = Get-Content (Join-Path $root "src/Link.Windows.Installer/App.xaml.cs") -Raw
if (-not $uninstall.Contains("--uninstall") -or -not $uninstall.Contains("--purge-data")) {
  throw "Uninstall/data-preservation command paths are incomplete."
}

Write-Host "Installer P0-P13 source, security and packaging-readiness checks passed." -ForegroundColor Green

if ($Build) {
  dotnet build (Join-Path $root "Link.Windows.Installer.sln") -c Debug
  if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

  dotnet run --project (Join-Path $root "tests/Link.Windows.Installer.Core.SmokeTests/Link.Windows.Installer.Core.SmokeTests.csproj") -c Debug --no-build
  if ($LASTEXITCODE -ne 0) { throw "Core smoke tests failed." }
}
