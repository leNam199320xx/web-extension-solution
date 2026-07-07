using PluginRuntime.Core.Enums;

namespace PluginRuntime.Core.Interfaces;

public record PermissionReviewRecord(
    Guid ReviewId,
    Guid VersionId,
    string Permissions,
    string RiskSummary,
    string? PermissionDiff,
    RiskLevel OverallRiskLevel,
    Guid? ReviewerId,
    ApprovalDecision? Decision,
    string? Comment,
    string? Conditions,
    DateTime? DecidedAt,
    DateTime CreatedAt);

public interface IPermissionReviewRepository
{
    Task<PermissionReviewRecord?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PermissionReviewRecord>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken);
    Task AddAsync(PermissionReviewRecord review, CancellationToken cancellationToken);
    Task UpdateAsync(PermissionReviewRecord review, CancellationToken cancellationToken);
}
