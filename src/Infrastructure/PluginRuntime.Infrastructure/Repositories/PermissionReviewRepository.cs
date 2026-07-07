using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class PermissionReviewRepository : IPermissionReviewRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public PermissionReviewRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PermissionReviewRecord?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PermissionReviews
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<PermissionReviewRecord>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.PermissionReviews
            .Where(r => r.VersionId == versionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToList();
    }

    public async Task AddAsync(PermissionReviewRecord review, CancellationToken cancellationToken)
    {
        var entity = new PermissionReviewEntity
        {
            ReviewId = review.ReviewId,
            VersionId = review.VersionId,
            Permissions = review.Permissions,
            RiskSummary = review.RiskSummary,
            PermissionDiff = review.PermissionDiff,
            OverallRiskLevel = review.OverallRiskLevel,
            ReviewerId = review.ReviewerId,
            Decision = review.Decision,
            Comment = review.Comment,
            Conditions = review.Conditions,
            DecidedAt = review.DecidedAt,
            CreatedAt = review.CreatedAt
        };

        await _dbContext.PermissionReviews.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PermissionReviewRecord review, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PermissionReviews
            .FirstOrDefaultAsync(r => r.ReviewId == review.ReviewId, cancellationToken);

        if (entity is not null)
        {
            entity.Permissions = review.Permissions;
            entity.RiskSummary = review.RiskSummary;
            entity.PermissionDiff = review.PermissionDiff;
            entity.OverallRiskLevel = review.OverallRiskLevel;
            entity.ReviewerId = review.ReviewerId;
            entity.Decision = review.Decision;
            entity.Comment = review.Comment;
            entity.Conditions = review.Conditions;
            entity.DecidedAt = review.DecidedAt;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static PermissionReviewRecord ToRecord(PermissionReviewEntity entity)
    {
        return new PermissionReviewRecord(
            entity.ReviewId,
            entity.VersionId,
            entity.Permissions,
            entity.RiskSummary,
            entity.PermissionDiff,
            entity.OverallRiskLevel,
            entity.ReviewerId,
            entity.Decision,
            entity.Comment,
            entity.Conditions,
            entity.DecidedAt,
            entity.CreatedAt);
    }
}
