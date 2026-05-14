using POC.CustomerValidation.API.Interfaces;

namespace POC.CustomerValidation.API.Services.Provisioning;

/// <summary>
/// Ensures every organisation has a blob storage container on API startup.
/// Uses CreateIfNotExistsAsync, so it is safe to run on every restart.
/// New orgs are provisioned immediately in OrganizationServices.CreateAsync;
/// this service covers orgs that existed before blob provisioning was added.
/// </summary>
public class StartupBlobProvisioningService(
    IServiceProvider services,
    ILogger<StartupBlobProvisioningService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope        = services.CreateScope();
        var orgRepo            = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var storageService     = scope.ServiceProvider.GetRequiredService<IOrganizationStorageService>();

        IEnumerable<POC.CustomerValidation.API.Models.Entites.Organization> orgs;
        try
        {
            orgs = await orgRepo.GetAllAsync(includeInactive: true);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not load organizations for startup blob provisioning — skipping");
            return;
        }

        var orgList = orgs.ToList();
        log.LogInformation("Startup blob provisioning — checking {Count} organizations", orgList.Count);

        int ok = 0, skipped = 0;
        foreach (var org in orgList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await storageService.ProvisionContainerAsync(org.OrganizationId, org.Abbreviation);
                ok++;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex,
                    "Could not provision blob container for org {OrgId} ({Abbr}) — " +
                    "check AzureStorage:ConnectionString or start Azurite",
                    org.OrganizationId, org.Abbreviation);
                skipped++;
            }
        }

        log.LogInformation("Startup blob provisioning complete — {Ok} OK, {Skipped} failed", ok, skipped);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
