using System.Diagnostics;
using System.Net.Sockets;

namespace POC.CustomerValidation.API.Services.Provisioning;

/// <summary>
/// Development-only hosted service that starts Azurite automatically when the API launches.
/// Only registered when ASPNETCORE_ENVIRONMENT = Development.
/// Skips startup if port 10000 is already in use (Azurite already running).
/// </summary>
internal sealed class AzuriteHostedService(
    IConfiguration configuration,
    ILogger<AzuriteHostedService> logger) : IHostedService, IDisposable
{
    private Process? _process;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsPortInUse(10000))
        {
            logger.LogInformation("Azurite already running on port 10000 — skipping auto-start");
            return Task.CompletedTask;
        }

        var location = configuration["AzureStorage:AzuriteLocation"] ?? @"C:\azurite";
        Directory.CreateDirectory(location);

        var debugLog = Path.Combine(location, "debug.log");

        try
        {
            // UseShellExecute = false won't resolve .cmd files (azurite is azurite.cmd from npm).
            // Route through cmd.exe so the PATH resolution works the same as a terminal.
            _process = Process.Start(new ProcessStartInfo
            {
                FileName        = "cmd.exe",
                Arguments       = $"/c azurite --silent --location \"{location}\" --debug \"{debugLog}\"",
                UseShellExecute = false,
                CreateNoWindow  = true,
            });

            logger.LogInformation("Azurite started (pid {Pid}) — data at {Location}", _process?.Id, location);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Could not auto-start Azurite. Run manually: azurite --silent --location \"{Location}\" --debug \"{DebugLog}\"",
                location, debugLog);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                logger.LogInformation("Azurite stopped");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not stop Azurite process cleanly");
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose() => _process?.Dispose();

    private static bool IsPortInUse(int port)
    {
        try
        {
            using var tcp = new TcpClient();
            tcp.Connect("127.0.0.1", port);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
