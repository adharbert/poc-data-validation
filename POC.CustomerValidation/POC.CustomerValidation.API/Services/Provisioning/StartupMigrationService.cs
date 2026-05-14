namespace POC.CustomerValidation.API.Services.Provisioning;

/// <summary>
/// Runs DbUp against every isolated-database org once at API startup.
/// DbUp's journal (SchemaVersions table) ensures each script runs only once
/// per database, so this is safe to run on every startup — it's a no-op when
/// all databases are already up to date.
/// </summary>
public class StartupMigrationService(
    IServiceProvider services,
    ILogger<StartupMigrationService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope     = services.CreateScope();
        var provisioner     = scope.ServiceProvider.GetRequiredService<IOrganizationProvisioningService>();

        log.LogInformation("Running startup migrations on all isolated databases");

        var results = await provisioner.MigrateAllIsolatedAsync(cancellationToken);

        foreach (var r in results)
        {
            if (r.Success)
                log.LogInformation("Migrations OK — {OrgName} ({OrgId})", r.OrganizationName, r.OrganizationId);
            else
                log.LogError("Migration FAILED — {OrgName} ({OrgId}): {Error}", r.OrganizationName, r.OrganizationId, r.Error);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
