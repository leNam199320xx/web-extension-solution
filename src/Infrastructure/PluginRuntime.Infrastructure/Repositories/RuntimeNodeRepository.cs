using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class RuntimeNodeRepository : IRuntimeNodeRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public RuntimeNodeRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RuntimeNodeRecord?> GetByIdAsync(string nodeId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.RuntimeNodes
            .FirstOrDefaultAsync(n => n.NodeId == nodeId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<RuntimeNodeRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await _dbContext.RuntimeNodes
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToList();
    }

    public async Task AddAsync(RuntimeNodeRecord node, CancellationToken cancellationToken)
    {
        var entity = new RuntimeNodeEntity
        {
            NodeId = node.NodeId,
            Hostname = node.Hostname,
            Version = node.Version,
            Status = node.Status,
            StartedAt = node.StartedAt,
            LastHeartbeat = node.LastHeartbeat
        };

        await _dbContext.RuntimeNodes.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RuntimeNodeRecord node, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.RuntimeNodes
            .FirstOrDefaultAsync(n => n.NodeId == node.NodeId, cancellationToken);

        if (entity is not null)
        {
            entity.Hostname = node.Hostname;
            entity.Version = node.Version;
            entity.Status = node.Status;
            entity.StartedAt = node.StartedAt;
            entity.LastHeartbeat = node.LastHeartbeat;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static RuntimeNodeRecord ToRecord(RuntimeNodeEntity entity)
    {
        return new RuntimeNodeRecord(
            entity.NodeId,
            entity.Hostname,
            entity.Version,
            entity.Status,
            entity.StartedAt,
            entity.LastHeartbeat);
    }
}
