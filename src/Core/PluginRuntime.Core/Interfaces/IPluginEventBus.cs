using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Core.Interfaces;

public interface IPluginEventBus
{
    Task PublishAsync(PluginEvent pluginEvent, CancellationToken cancellationToken);
    Task SubscribeAsync(string eventType, Func<PluginEvent, Task> handler, CancellationToken cancellationToken);
}
