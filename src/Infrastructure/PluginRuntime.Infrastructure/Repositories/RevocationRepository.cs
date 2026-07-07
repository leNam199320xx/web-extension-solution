using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class RevocationRepository : IRevocationRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public RevocationRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RevocationRecord?> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Revocations
            .FirstOrDefaultAsync(r => r.VersionId == versionId, cancellationToken);

        return entity is null ? null : new RevocationRecord(
            entity.RevocationId,
            entity.VersionId,
            entity.Reason,
            entity.RevokedBy,
            entity.RevokedAt,
            entity.ExpiresAt);
    }
}
