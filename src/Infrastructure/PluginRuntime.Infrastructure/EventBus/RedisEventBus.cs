using System.Collections.Concurrent;
using System.Text.Json;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;
using StackExchange.Redis;

namespace PluginRuntime.Infrastructure.EventBus;

/// <summary>
/// Redis Pub/Sub event bus for cross-instance coordination.
/// Broadcasts hot-reload signals, plugin state changes across all runtime instances.
/// For single-instance deployment, use <see cref="InMemoryEventBus"/> instead.
/// </summary>
public sealed class RedisEventBus : IPluginEventBus, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ConcurrentDictionary<string, List<Func<PluginEvent, Task>>> _handlers = new();
    private readonly ConcurrentDictionary<string, bool> _subscribedChannels = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    private const string ChannelPrefix = "plugin-events:";

    public RedisEventBus(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task PublishAsync(PluginEvent pluginEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(pluginEvent);

        var channel = $"{ChannelPrefix}{pluginEvent.EventType}";
        var message = JsonSerializer.Serialize(pluginEvent, _jsonOptions);

        var subscriber = _redis.GetSubscriber();
        await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
    }

    public async Task SubscribeAsync(string eventType, Func<PluginEvent, Task> handler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = _handlers.GetOrAdd(eventType, _ => new List<Func<PluginEvent, Task>>());

        lock (handlers)
        {
            handlers.Add(handler);
        }

        // Subscribe to the Redis channel only once per event type
        if (_subscribedChannels.TryAdd(eventType, true))
        {
            var channel = $"{ChannelPrefix}{eventType}";
            var subscriber = _redis.GetSubscriber();

            await subscriber.SubscribeAsync(RedisChannel.Literal(channel), async (_, message) =>
            {
                if (message.IsNullOrEmpty)
                    return;

                try
                {
                    var pluginEvent = JsonSerializer.Deserialize<PluginEvent>(message.ToString(), _jsonOptions);
                    if (pluginEvent is not null)
                    {
                        await DispatchToHandlersAsync(eventType, pluginEvent);
                    }
                }
                catch
                {
                    // Deserialization or handler failure should not break the subscription.
                    // In production, this would be logged via ILogger.
                }
            });
        }
    }

    private async Task DispatchToHandlersAsync(string eventType, PluginEvent pluginEvent)
    {
        if (_handlers.TryGetValue(eventType, out var handlers))
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
        if (_disposed)
            return;

        _disposed = true;

        var subscriber = _redis.GetSubscriber();
        foreach (var eventType in _subscribedChannels.Keys)
        {
            var channel = $"{ChannelPrefix}{eventType}";
            subscriber.Unsubscribe(RedisChannel.Literal(channel));
        }
    }
}
