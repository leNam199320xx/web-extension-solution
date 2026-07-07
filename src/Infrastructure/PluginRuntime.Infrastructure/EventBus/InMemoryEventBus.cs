using System.Collections.Concurrent;
using System.Threading.Channels;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Infrastructure.EventBus;

/// <summary>
/// In-memory event bus using Channel&lt;PluginEvent&gt; for in-process pub/sub.
/// Suitable for single-instance deployment and testing.
/// For multi-instance deployment, swap to <see cref="RedisEventBus"/> via DI configuration.
/// </summary>
public sealed class InMemoryEventBus : IPluginEventBus, IDisposable
{
    private readonly ConcurrentDictionary<string, List<Func<PluginEvent, Task>>> _handlers = new();
    private readonly Channel<PluginEvent> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _readerTask;

    public InMemoryEventBus()
    {
        _channel = Channel.CreateBounded<PluginEvent>(new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        _readerTask = Task.Run(ProcessEventsAsync);
    }

    public async Task PublishAsync(PluginEvent pluginEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(pluginEvent);

        await _channel.Writer.WriteAsync(pluginEvent, cancellationToken);
    }

    public Task SubscribeAsync(string eventType, Func<PluginEvent, Task> handler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = _handlers.GetOrAdd(eventType, _ => new List<Func<PluginEvent, Task>>());

        lock (handlers)
        {
            handlers.Add(handler);
        }

        return Task.CompletedTask;
    }

    private async Task ProcessEventsAsync()
    {
        try
        {
            await foreach (var pluginEvent in _channel.Reader.ReadAllAsync(_cts.Token))
            {
                await DispatchEventAsync(pluginEvent);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested — exit gracefully
        }
    }

    private async Task DispatchEventAsync(PluginEvent pluginEvent)
    {
        if (_handlers.TryGetValue(pluginEvent.EventType, out var handlers))
        {
            Func<PluginEvent, Task>[] snapshot;
            lock (handlers)
            {
                snapshot = [.. handlers];
            }

            foreach (var handler in snapshot)
            {
                try
                {
                    await handler(pluginEvent);
                }
                catch
                {
                    // Individual handler failures should not break the event bus.
                    // In production, this would be logged via ILogger.
                }
            }
        }
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();
        _cts.Dispose();
    }
}
