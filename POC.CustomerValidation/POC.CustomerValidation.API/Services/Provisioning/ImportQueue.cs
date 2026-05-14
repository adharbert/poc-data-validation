using System.Threading.Channels;

namespace POC.CustomerValidation.API.Services.Provisioning;

public class ImportQueue : IImportQueue
{
    private readonly Channel<ImportQueueItem> _channel = Channel.CreateUnbounded<ImportQueueItem>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(ImportQueueItem item) => _channel.Writer.TryWrite(item);

    public IAsyncEnumerable<ImportQueueItem> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
