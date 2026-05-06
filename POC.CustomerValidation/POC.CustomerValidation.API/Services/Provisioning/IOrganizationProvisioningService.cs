namespace POC.CustomerValidation.API.Services.Provisioning;

public record DatabaseMigrationResult(
    Guid    OrganizationId,
    string  OrganizationName,
    bool    Success,
    string? Error
);

public interface IOrganizationProvisioningService
{
    Task ProvisionAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Runs DbUp against every isolated database that is in 'ready' status.
    /// Only scripts not yet in the DbUp journal are applied — existing databases
    /// are never re-run from scratch.
    /// </summary>
    Task<IEnumerable<DatabaseMigrationResult>> MigrateAllIsolatedAsync(CancellationToken ct = default);
}
