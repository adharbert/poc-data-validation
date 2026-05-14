using POC.CustomerValidation.API.Interfaces;

namespace POC.CustomerValidation.API.Services.Provisioning;

public class BlobImportPollingService(
    IServiceProvider services,
    IConfiguration config,
    ILogger<BlobImportPollingService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(
            config.GetValue<int>("ImportSettings:BlobPollIntervalSeconds", 300));

        log.LogInformation("Blob import polling started — interval {Seconds}s", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Blob import poll cycle failed");
            }
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var scope        = services.CreateScope();
        var importService      = scope.ServiceProvider.GetRequiredService<IImportService>();
        var orgRepo            = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var projectRepo        = scope.ServiceProvider.GetRequiredService<IMarketingProjectRepository>();
        var storageService     = scope.ServiceProvider.GetRequiredService<IOrganizationStorageService>();

        var orgs = await orgRepo.GetAllAsync(includeInactive: false);

        foreach (var org in orgs)
        {
            var containerName = storageService.GetContainerName(org.OrganizationId, org.Abbreviation);
            var projects      = await projectRepo.GetByOrganisationIdAsync(org.OrganizationId, includeInactive: false);

            foreach (var project in projects)
            {
                var prefix = $"imports/{project.ProjectId}/";

                try
                {
                    await foreach (var blobPath in storageService.ListBlobsAsync(containerName, prefix).WithCancellation(ct))
                    {
                        if (blobPath.EndsWith(".keep", StringComparison.OrdinalIgnoreCase)) continue;

                        var ext = Path.GetExtension(blobPath).TrimStart('.').ToLowerInvariant();
                        if (ext is not ("csv" or "xlsx" or "xls")) continue;

                        var fileName = Path.GetFileName(blobPath);
                        try
                        {
                            var created = await importService.CreateBatchFromBlobAsync(
                                org.OrganizationId, project.ProjectId,
                                containerName, blobPath, fileName, "sftp");

                            if (created is not null)
                                log.LogInformation("Auto-registered SFTP file {BlobPath} as import batch {BatchId}",
                                    blobPath, created.BatchId);
                        }
                        catch (Exception ex)
                        {
                            log.LogWarning(ex, "Failed to register SFTP file {BlobPath}", blobPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Failed to list blobs for org {OrgId} project {ProjectId}", org.OrganizationId, project.ProjectId);
                }
            }
        }
    }
}
