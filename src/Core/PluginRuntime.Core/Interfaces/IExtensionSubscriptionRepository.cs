using PluginRuntime.Core.Enums;

namespace PluginRuntime.Core.Interfaces;

public record ExtensionSubscriptionRecord(
    Guid SubscriptionId,
    string SourceExtensionId,
    string TargetExtensionId,
    SubscriptionStatus Status,
    string? Reason,
    string? ExpectedUsage,
    string? Conditions,
    Guid? DecidedBy,
    DateTime? DecidedAt,
    DateTime? ExpiresAt,
    DateTime? RevokedAt,
    DateTime CreatedAt);

public interface IExtensionSubscriptionRepository
{
    Task<ExtensionSubscriptionRecord?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken);
    Task<ExtensionSubscriptionRecord?> GetBySourceAndTargetAsync(string sourceExtensionId, string targetExtensionId, CancellationToken cancellationToken);
    Task AddAsync(ExtensionSubscriptionRecord subscription, CancellationToken cancellationToken);
    Task UpdateAsync(ExtensionSubscriptionRecord subscription, CancellationToken cancellationToken);
}
