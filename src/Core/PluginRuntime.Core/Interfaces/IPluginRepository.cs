using PluginRuntime.Core.Entities;

namespace PluginRuntime.Core.Interfaces;

public interface IPluginRepository
{
    Task<Plugin?> GetByIdAsync(Guid pluginId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Plugin>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Plugin plugin, CancellationToken cancellationToken);
    Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken);
    Task DeleteAsync(Guid pluginId, CancellationToken cancellationToken);
}
