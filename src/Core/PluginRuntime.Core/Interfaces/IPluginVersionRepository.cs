namespace PluginRuntime.Core.Interfaces;

public interface IPluginVersionRepository
{
    Task<Entities.PluginVersion?> GetLatestApprovedAsync(Guid pluginId, CancellationToken cancellationToken);
    Task<Entities.PluginVersion?> GetByVersionAsync(Guid pluginId, string version, CancellationToken cancellationToken);
}
