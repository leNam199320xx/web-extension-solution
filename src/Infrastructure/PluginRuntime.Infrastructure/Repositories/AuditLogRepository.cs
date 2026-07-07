using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public AuditLogRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLogRecord auditLog, CancellationToken cancellationToken)
    {
        var entity = new AuditLogEntity
        {
            AuditId = auditLog.AuditId,
            Timestamp = auditLog.Timestamp,
            ActorId = auditLog.ActorId,
            ActorType = auditLog.ActorType,
            Action = auditLog.Action,
            ResourceType = auditLog.ResourceType,
            ResourceId = auditLog.ResourceId,
            IpAddress = auditLog.IpAddress,
            Result = auditLog.Result,
            Metadata = auditLog.Metadata
        };

        await _dbContext.AuditLogs.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogRecord>> GetByResourceAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.AuditLogs
            .Where(a => a.ResourceType == resourceType && a.ResourceId == resourceId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new AuditLogRecord(
            e.AuditId,
            e.Timestamp,
            e.ActorId,
            e.ActorType,
            e.Action,
            e.ResourceType,
            e.ResourceId,
            e.IpAddress,
            e.Result,
            e.Metadata)).ToList();
    }
}
