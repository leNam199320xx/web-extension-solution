using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class CapabilityRepository : ICapabilityRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public CapabilityRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CapabilityRecord?> GetByIdAsync(Guid capabilityId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Capabilities
            .FirstOrDefaultAsync(c => c.CapabilityId == capabilityId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<CapabilityRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Capabilities
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToList();
    }

    public async Task AddAsync(CapabilityRecord capability, CancellationToken cancellationToken)
    {
        var entity = new CapabilityEntity
        {
            CapabilityId = capability.CapabilityId,
            Name = capability.Name,
            Version = capability.Version,
            Category = capability.Category,
            Description = capability.Description,
            Enabled = capability.Enabled,
            CreatedAt = capability.CreatedAt
        };

        await _dbContext.Capabilities.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static CapabilityRecord ToRecord(CapabilityEntity entity)
    {
        return new CapabilityRecord(
            entity.CapabilityId,
            entity.Name,
            entity.Version,
            entity.Category,
            entity.Description,
            entity.Enabled,
            entity.CreatedAt);
    }
}
