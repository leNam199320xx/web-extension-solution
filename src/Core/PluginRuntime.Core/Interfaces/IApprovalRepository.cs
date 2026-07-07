using PluginRuntime.Core.Enums;

namespace PluginRuntime.Core.Interfaces;

public record ApprovalRecord(
    Guid ApprovalId,
    Guid VersionId,
    Guid ReviewerId,
    ApprovalDecision Decision,
    string? Comment,
    DateTime DecidedAt);

public interface IApprovalRepository
{
    Task<ApprovalRecord?> GetByIdAsync(Guid approvalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApprovalRecord>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken);
    Task AddAsync(ApprovalRecord approval, CancellationToken cancellationToken);
}
