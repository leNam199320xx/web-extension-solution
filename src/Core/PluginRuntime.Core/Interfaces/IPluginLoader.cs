using PluginRuntime.Core.Entities;
using PluginRuntime.Sdk;

namespace PluginRuntime.Core.Interfaces;

public interface IPluginLoader
{
    Task<IPlugin> LoadAsync(PluginVersion version, Manifest manifest, CancellationToken cancellationToken);
    Task UnloadAsync(string pluginId, string version);
}
