using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Enums;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;

namespace PluginRuntime.Infrastructure.Repositories;

public class PluginVersionRepository : IPluginVersionRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public PluginVersionRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PluginVersion?> GetLatestApprovedAsync(Guid pluginId, CancellationToken cancellationToken)
    {
        return await _dbContext.PluginVersions
            .Where(v => v.PluginId == pluginId && v.Status == PluginVersionStatus.Approved)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PluginVersion?> GetByVersionAsync(Guid pluginId, string version, CancellationToken cancellationToken)
    {
        return await _dbContext.PluginVersions
            .FirstOrDefaultAsync(v => v.PluginId == pluginId && v.Version == version, cancellationToken);
    }
}
