using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using Link.Windows.Installer.Core.Models;

namespace Link.Windows.Installer.Services;

public sealed class DatabaseConnectivityService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(6);

    public async Task<DatabaseValidationResult> TestCloudEndpointAsync(
        string backendUrl,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(backendUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return new(DatabaseValidationState.Invalid, "Backend URL is invalid.", "Enter a valid HTTP/HTTPS backend URL.");

        var port = uri.IsDefaultPort
            ? uri.Scheme == Uri.UriSchemeHttps ? 443 : 80
            : uri.Port;

        return await TestTcpAsync(
            uri.Host,
            port,
            "Backend endpoint reachable",
            $"TCP connectivity to {uri.Host}:{port} succeeded. Authenticated application/API validation occurs after sign-in.",
            cancellationToken);
    }

    public async Task<DatabaseValidationResult> TestPostgreSqlAsync(
        string host,
        int port,
        string database,
        string user,
        string password,
        string? psqlPath,
        CancellationToken cancellationToken = default)
    {
        var tcp = await TestTcpAsync(
            host,
            port,
            "PostgreSQL endpoint reachable",
            $"TCP connectivity to {host}:{port} succeeded.",
            cancellationToken);

        if (!tcp.IsSuccessful)
            return tcp;

        if (string.IsNullOrWhiteSpace(psqlPath) || !File.Exists(psqlPath))
            return new(
                DatabaseValidationState.Warning,
                "PostgreSQL endpoint reachable; authentication test pending.",
                "psql.exe was not found, so this pass validated network reachability only.");

        if (string.IsNullOrWhiteSpace(password))
            return new(
                DatabaseValidationState.Warning,
                "PostgreSQL endpoint reachable; password not supplied.",
                "Enter the database password to run an authenticated SELECT 1 self-test.");

        try
        {
            var start = new ProcessStartInfo
            {
                FileName = psqlPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            start.ArgumentList.Add("--host");
            start.ArgumentList.Add(host);
            start.ArgumentList.Add("--port");
            start.ArgumentList.Add(port.ToString());
            start.ArgumentList.Add("--dbname");
            start.ArgumentList.Add(database);
            start.ArgumentList.Add("--username");
            start.ArgumentList.Add(user);
            start.ArgumentList.Add("--no-password");
            start.ArgumentList.Add("--tuples-only");
            start.ArgumentList.Add("--command");
            start.ArgumentList.Add("SELECT 1;");

            start.Environment["PGPASSWORD"] = password;
            start.Environment["PGCONNECT_TIMEOUT"] = "5";

            using var process = Process.Start(start);
            if (process is null)
                return new(DatabaseValidationState.Invalid, "Could not start PostgreSQL validation.", "psql.exe failed to start.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            await process.WaitForExitAsync(timeout.Token);

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            return process.ExitCode == 0 && stdout.Contains('1')
                ? new(DatabaseValidationState.Valid, "Authenticated PostgreSQL test passed.", "SELECT 1 completed with the configured restricted database identity.")
                : new(DatabaseValidationState.Invalid, "PostgreSQL authentication test failed.", SanitisePostgresError(stderr));
        }
        catch (OperationCanceledException)
        {
            return new(DatabaseValidationState.Invalid, "PostgreSQL validation timed out.", "The database did not complete the validation within the allowed time.");
        }
        catch (Exception ex)
        {
            return new(DatabaseValidationState.Invalid, "PostgreSQL validation failed.", ex.Message);
        }
    }

    private static async Task<DatabaseValidationResult> TestTcpAsync(
        string host,
        int port,
        string successSummary,
        string successDetail,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            await client.ConnectAsync(host, port, timeout.Token);
            return new(DatabaseValidationState.Valid, successSummary, successDetail);
        }
        catch (OperationCanceledException)
        {
            return new(DatabaseValidationState.Invalid, "Connection timed out.", $"Could not reach {host}:{port} within {Timeout.TotalSeconds:0} seconds.");
        }
        catch (Exception ex)
        {
            return new(DatabaseValidationState.Invalid, "Connection failed.", $"{host}:{port} • {ex.Message}");
        }
    }

    private static string SanitisePostgresError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "psql returned a non-zero exit code.";

        var lines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.Contains("password", StringComparison.OrdinalIgnoreCase))
            .Take(4);

        return string.Join(" ", lines);
    }
}
