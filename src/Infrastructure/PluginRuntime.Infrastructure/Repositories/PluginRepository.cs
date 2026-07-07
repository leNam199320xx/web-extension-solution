using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;

namespace PluginRuntime.Infrastructure.Repositories;

public class PluginRepository : IPluginRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public PluginRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Plugin?> GetByIdAsync(Guid pluginId, CancellationToken cancellationToken)
    {
        return await _dbContext.Plugins
            .FirstOrDefaultAsync(p => p.PluginId == pluginId, cancellationToken);
    }

    public async Task<IReadOnlyList<Plugin>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Plugins
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Plugin plugin, CancellationToken cancellationToken)
    {
        await _dbContext.Plugins.AddAsync(plugin, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken)
    {
        _dbContext.Plugins.Update(plugin);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid pluginId, CancellationToken cancellationToken)
    {
        var plugin = await _dbContext.Plugins
            .FirstOrDefaultAsync(p => p.PluginId == pluginId, cancellationToken);

        if (plugin is not null)
        {
            _dbContext.Plugins.Remove(plugin);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
