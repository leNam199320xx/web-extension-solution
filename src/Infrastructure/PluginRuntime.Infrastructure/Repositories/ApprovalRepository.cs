using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class ApprovalRepository : IApprovalRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public ApprovalRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApprovalRecord?> GetByIdAsync(Guid approvalId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Approvals
            .FirstOrDefaultAsync(a => a.ApprovalId == approvalId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<ApprovalRecord>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Approvals
            .Where(a => a.VersionId == versionId)
            .OrderByDescending(a => a.DecidedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToList();
    }

    public async Task AddAsync(ApprovalRecord approval, CancellationToken cancellationToken)
    {
        var entity = new ApprovalEntity
        {
            ApprovalId = approval.ApprovalId,
            VersionId = approval.VersionId,
            ReviewerId = approval.ReviewerId,
            Decision = approval.Decision,
            Comment = approval.Comment,
            DecidedAt = approval.DecidedAt
        };

        await _dbContext.Approvals.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ApprovalRecord ToRecord(ApprovalEntity entity)
    {
        return new ApprovalRecord(
            entity.ApprovalId,
            entity.VersionId,
            entity.ReviewerId,
            entity.Decision,
            entity.Comment,
            entity.DecidedAt);
    }
}
