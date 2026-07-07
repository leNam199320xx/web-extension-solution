using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class ExtensionSubscriptionRepository : IExtensionSubscriptionRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public ExtensionSubscriptionRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExtensionSubscriptionRecord?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ExtensionSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<ExtensionSubscriptionRecord?> GetBySourceAndTargetAsync(string sourceExtensionId, string targetExtensionId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ExtensionSubscriptions
            .FirstOrDefaultAsync(s => s.SourceExtensionId == sourceExtensionId && s.TargetExtensionId == targetExtensionId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task AddAsync(ExtensionSubscriptionRecord subscription, CancellationToken cancellationToken)
    {
        var entity = new ExtensionSubscriptionEntity
        {
            SubscriptionId = subscription.SubscriptionId,
            SourceExtensionId = subscription.SourceExtensionId,
            TargetExtensionId = subscription.TargetExtensionId,
            Status = subscription.Status,
            Reason = subscription.Reason,
            ExpectedUsage = subscription.ExpectedUsage,
            Conditions = subscription.Conditions,
            DecidedBy = subscription.DecidedBy,
            DecidedAt = subscription.DecidedAt,
            ExpiresAt = subscription.ExpiresAt,
            RevokedAt = subscription.RevokedAt,
            CreatedAt = subscription.CreatedAt
        };

        await _dbContext.ExtensionSubscriptions.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExtensionSubscriptionRecord subscription, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ExtensionSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscription.SubscriptionId, cancellationToken);

        if (entity is not null)
        {
            entity.Status = subscription.Status;
            entity.Reason = subscription.Reason;
            entity.ExpectedUsage = subscription.ExpectedUsage;
            entity.Conditions = subscription.Conditions;
            entity.DecidedBy = subscription.DecidedBy;
            entity.DecidedAt = subscription.DecidedAt;
            entity.ExpiresAt = subscription.ExpiresAt;
            entity.RevokedAt = subscription.RevokedAt;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static ExtensionSubscriptionRecord ToRecord(ExtensionSubscriptionEntity entity)
    {
        return new ExtensionSubscriptionRecord(
            entity.SubscriptionId,
            entity.SourceExtensionId,
            entity.TargetExtensionId,
            entity.Status,
            entity.Reason,
            entity.ExpectedUsage,
            entity.Conditions,
            entity.DecidedBy,
            entity.DecidedAt,
            entity.ExpiresAt,
            entity.RevokedAt,
            entity.CreatedAt);
    }
}
