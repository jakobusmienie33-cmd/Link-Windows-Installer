using Link.Windows.Installer.Core.Models;
using Link.Windows.Installer.Core.Services;

var evaluator = new SystemCompatibilityEvaluator();

var supported = new SystemSnapshot(
    true, "Windows 11 Pro", 26100, "X64", 10_000_000_000,
    16L * 1024 * 1024 * 1024, true, false, false,
    "No local PostgreSQL installation detected.");

var supportedResults = evaluator.Evaluate(supported);
Assert(!supportedResults.Any(result => result.State == SystemCheckState.Block),
    "Supported workstation unexpectedly produced a blocking result.");
Assert(supportedResults.Single(result => result.Name == "PostgreSQL").Badge == "Not Required",
    "Standard workstation PostgreSQL absence must remain non-blocking.");

var unsupported = new SystemSnapshot(
    true, "Windows 10", 15000, "X86", 500_000_000,
    2L * 1024 * 1024 * 1024, false, false, false,
    "No local PostgreSQL installation detected.");

var unsupportedResults = evaluator.Evaluate(unsupported);
Assert(unsupportedResults.Count(result => result.State == SystemCheckState.Block) >= 4,
    "Unsupported snapshot should block on OS, architecture, disk and memory.");

var layoutPolicy = new InstallationLayoutPolicy();
var validLayout = new InstallationLayout(
    @"C:\Program Files\The Link",
    @"C:\ProgramData\The Link",
    @"C:\ProgramData\The Link\Data",
    @"C:\ProgramData\The Link\Logs\Setup",
    @"C:\ProgramData\The Link\Backups",
    true,
    "The Link");

var layoutResult = layoutPolicy.Validate(validLayout, 5_000_000_000, false);
Assert(layoutResult.IsValid, "Canonical per-machine install layout should be valid.");
Assert(layoutResult.Warnings.Any(w => w.Contains("elevation", StringComparison.OrdinalIgnoreCase)),
    "Non-elevated per-machine planning should produce an elevation note.");

var invalidLayout = validLayout with { DataDirectory = validLayout.ApplicationDirectory };
Assert(!layoutPolicy.Validate(invalidLayout, 5_000_000_000, true).IsValid,
    "Application and mutable data directories must not be identical.");

var prerequisiteEvaluator = new PrerequisiteEvaluator();
var prerequisiteChecks = new[]
{
    new PrerequisiteCheck("a", "Bundled", "Bundled", "1", PrerequisiteState.Bundled, true, "Use"),
    new PrerequisiteCheck("b", "Planned", "Planned", "1", PrerequisiteState.Planned, true, "Install later")
};
Assert(!prerequisiteEvaluator.HasBlockingItems(prerequisiteChecks),
    "Signed-payload planned prerequisites must not be treated as missing blockers.");

var dbPlanner = new DatabaseSetupPlanner();
var cloudOptions = new DatabaseSetupOptions(
    DatabaseMode.CloudSupabaseWithLocalSqlite,
    "https://example.supabase.co",
    "localhost",
    5432,
    "the_link",
    "link_runtime",
    @"C:\ProgramData\The Link\Data");

Assert(dbPlanner.Validate(cloudOptions).Count == 0,
    "Valid cloud + SQLite configuration should pass planning validation.");

var cloudPlan = dbPlanner.CreatePlan(cloudOptions);
Assert(!cloudPlan.AllowsPrivilegedLocalBootstrap,
    "Normal cloud workstation mode must never enable privileged database bootstrap.");

var bootstrapPlanner = new DatabaseBootstrapPlanner();
var cloudBootstrap = bootstrapPlanner.Create(cloudOptions);
Assert(!cloudBootstrap.RunsProductionCloudDdl,
    "Windows installer must never plan production cloud DDL.");

var postgresOptions = cloudOptions with
{
    Mode = DatabaseMode.ExistingPostgreSql,
    BackendUrl = string.Empty,
    Host = "localhost",
    DatabaseName = "the_link_branch",
    UserName = "link_runtime"
};

var postgresBootstrap = bootstrapPlanner.Create(postgresOptions);
Assert(postgresBootstrap.RequiresPrivilegedExecutor,
    "Approved local/branch PostgreSQL bootstrap should require a privileged migration executor.");
Assert(postgresBootstrap.SecurityControls.Any(x => x.Contains("SUPERUSER", StringComparison.OrdinalIgnoreCase)),
    "PostgreSQL bootstrap must explicitly prohibit elevated runtime role privileges.");

var manifest = new ReleaseManifest(
    "The Link",
    "1.0.0",
    "x64",
    "the_link.exe",
    new[]
    {
        new ReleaseFileEntry("the_link.exe", new string('a', 64), 1234),
        new ReleaseFileEntry("data/flutter_assets/AssetManifest.bin", new string('b', 64), 4321)
    });

var manifestValidator = new ReleaseManifestValidator();
Assert(manifestValidator.Validate(manifest).Count == 0,
    "Canonical Link-Core release manifest should validate.");

var unsafeManifest = manifest with
{
    Files = new[] { new ReleaseFileEntry("../outside.exe", new string('c', 64), 10) },
    EntryPoint = "../outside.exe"
};
Assert(manifestValidator.Validate(unsafeManifest).Count > 0,
    "Release manifest path traversal must be rejected.");

var deploymentPlanner = new DeploymentPlanner();
var installPlan = deploymentPlanner.Plan(
    manifest, null, @"C:\Program Files\The Link", @"C:\ProgramData\The Link\Backups");
Assert(installPlan.Mode == DeploymentMode.Install, "No installed state must produce Install mode.");

var installed = new InstalledState(
    "The Link", "1.0.0", "x64", "the_link.exe",
    @"C:\Program Files\The Link", @"C:\ProgramData\The Link",
    DateTimeOffset.UtcNow.ToString("O"), new string('d', 64), "Stable", true);

Assert(deploymentPlanner.DetermineMode(manifest, installed) == DeploymentMode.Repair,
    "Same-version payload must produce Repair mode.");

var upgradeManifest = manifest with { Version = "1.1.0" };
Assert(deploymentPlanner.DetermineMode(upgradeManifest, installed) == DeploymentMode.Upgrade,
    "Newer payload must produce Upgrade mode.");

var downgradeBlocked = false;
try
{
    deploymentPlanner.DetermineMode(manifest with { Version = "0.9.0" }, installed);
}
catch (InvalidOperationException)
{
    downgradeBlocked = true;
}
Assert(downgradeBlocked, "Normal installer must block downgrade.");

var uninstallPlan = new UninstallPlanner().Create(
    validLayout,
    new UninstallOptions(true, true, true, true, true, true, true));

Assert(uninstallPlan.RemovePaths.Contains(validLayout.ApplicationDirectory),
    "Uninstall should remove application files.");
Assert(uninstallPlan.PreservePaths.Contains(validLayout.DataDirectory),
    "Default uninstall must preserve local business/cache data.");

Console.WriteLine("Link.Windows.Installer.Core smoke tests passed.");
return 0;

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
