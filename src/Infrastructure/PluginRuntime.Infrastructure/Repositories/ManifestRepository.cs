using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;

namespace PluginRuntime.Infrastructure.Repositories;

public class ManifestRepository : IManifestRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public ManifestRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Manifest?> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken)
    {
        return await _dbContext.Manifests
            .FirstOrDefaultAsync(m => m.VersionId == versionId, cancellationToken);
    }
}
