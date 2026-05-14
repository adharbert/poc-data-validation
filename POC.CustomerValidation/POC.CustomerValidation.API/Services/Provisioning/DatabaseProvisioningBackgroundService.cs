namespace POC.CustomerValidation.API.Services.Provisioning;

public class DatabaseProvisioningBackgroundService(
    IProvisioningQueue queue,
    IServiceProvider services,
    ILogger<DatabaseProvisioningBackgroundService> log) : BackgroundService
{
    private readonly IProvisioningQueue _queue = queue;
    private readonly IServiceProvider _services = services;
    private readonly ILogger<DatabaseProvisioningBackgroundService> _log = log;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Database provisioning service started");

        await foreach (var orgId in _queue.ReadAllAsync(stoppingToken))
        {
            _log.LogInformation("Dequeued provisioning job for org {OrgId}", orgId);

            using var scope = _services.CreateScope();
            var provisioner = scope.ServiceProvider.GetRequiredService<IOrganizationProvisioningService>();

            try
            {
                await provisioner.ProvisionAsync(orgId, stoppingToken);
            }
            catch (Exception ex)
            {
                // Provisioner already set status to 'failed' and logged details — just swallow here
                _log.LogError(ex, "Provisioning job failed for org {OrgId}", orgId);
            }
        }
    }
}
