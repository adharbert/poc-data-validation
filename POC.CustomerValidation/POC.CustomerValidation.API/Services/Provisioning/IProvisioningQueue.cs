namespace POC.CustomerValidation.API.Services.Provisioning;

public interface IProvisioningQueue
{
    void Enqueue(Guid organizationId);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct);
}
