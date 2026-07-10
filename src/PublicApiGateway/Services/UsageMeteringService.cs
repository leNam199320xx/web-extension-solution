using System.Threading.Channels;
using Microsoft.Extensions.Options;
using PublicApiGateway.Configuration;
using PublicApiGateway.Models;

namespace PublicApiGateway.Services;

/// <summary>
/// Async usage metering using Channel&lt;UsageRecord&gt;.
/// Bounded capacity with DropOldest policy — never blocks the request pipeline.
/// </summary>
public sealed class UsageMeteringService : IUsageMeteringService
{
    private readonly Channel<UsageRecord> _channel;

    public UsageMeteringService(IOptions<GatewayOptions> options)
    {
        _channel = Channel.CreateBounded<UsageRecord>(new BoundedChannelOptions(options.Value.UsageBufferCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void Enqueue(UsageRecord record)
    {
        _channel.Writer.TryWrite(record);
    }

    public ChannelReader<UsageRecord> Reader => _channel.Reader;
}
