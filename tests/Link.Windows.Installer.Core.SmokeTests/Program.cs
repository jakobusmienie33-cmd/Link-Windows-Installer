using Link.Windows.Installer.Core.Models;
using Link.Windows.Installer.Core.Services;

var evaluator = new SystemCompatibilityEvaluator();

var supported = new SystemSnapshot(
    true,
    "Windows 11 Pro",
    26100,
    "X64",
    10_000_000_000,
    16L * 1024 * 1024 * 1024,
    true,
    false,
    false,
    "No local PostgreSQL installation detected.");

var supportedResults = evaluator.Evaluate(supported);
Assert(!supportedResults.Any(result => result.State == SystemCheckState.Block),
    "Supported workstation unexpectedly produced a blocking result.");
Assert(supportedResults.Single(result => result.Name == "PostgreSQL").Badge == "Not Required",
    "Standard workstation PostgreSQL absence must remain non-blocking.");

var unsupported = new SystemSnapshot(
    true,
    "Windows 10",
    15000,
    "X86",
    500_000_000,
    2L * 1024 * 1024 * 1024,
    false,
    false,
    false,
    "No local PostgreSQL installation detected.");

var unsupportedResults = evaluator.Evaluate(unsupported);
Assert(unsupportedResults.Count(result => result.State == SystemCheckState.Block) >= 4,
    "Unsupported snapshot should block on OS, architecture, disk and memory.");

Console.WriteLine("Link.Windows.Installer.Core smoke tests passed.");
return 0;

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
