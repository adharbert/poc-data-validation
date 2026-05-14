using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Persistence;

namespace POC.CustomerValidation.API.Services.Provisioning;

public class ImportProcessorBackgroundService(
    IImportQueue queue,
    ITenantConnectionCache tenantCache,
    IServiceProvider services,
    ILogger<ImportProcessorBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("Import processor background service started");

        await foreach (var item in queue.ReadAllAsync(stoppingToken))
        {
            log.LogInformation("Dequeued import batch {BatchId}", item.BatchId);

            using var scope = services.CreateScope();

            // Resolve tenant context for this import's organization so all
            // repository calls in this scope hit the correct (possibly isolated) database.
            var isolatedCs = await tenantCache.GetIsolatedConnectionStringAsync(item.OrganizationId);
            if (isolatedCs is not null)
            {
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tenantContext.Resolve(isolatedCs);
            }

            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            try
            {
                await importService.ExecuteAsync(item.BatchId);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Import batch {BatchId} failed in background processor", item.BatchId);
            }
        }
    }
}
