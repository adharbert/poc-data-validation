namespace POC.CustomerValidation.API.Services.Provisioning;

public record ImportQueueItem(Guid BatchId, Guid OrganizationId);

public interface IImportQueue
{
    void Enqueue(ImportQueueItem item);
    IAsyncEnumerable<ImportQueueItem> ReadAllAsync(CancellationToken ct);
}
