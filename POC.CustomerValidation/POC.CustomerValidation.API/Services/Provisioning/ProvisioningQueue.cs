using System.Threading.Channels;

namespace POC.CustomerValidation.API.Services.Provisioning;

public class ProvisioningQueue : IProvisioningQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Guid organizationId) => _channel.Writer.TryWrite(organizationId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
