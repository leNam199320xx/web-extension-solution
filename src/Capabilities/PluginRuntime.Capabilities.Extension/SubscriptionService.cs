using PluginRuntime.Core.Enums;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Extension;

/// <summary>
/// Manages extension subscription workflow: request, decide, check active status, revoke.
/// </summary>
public class SubscriptionService
{
    private readonly IExtensionSubscriptionRepository _subscriptionRepository;

    public SubscriptionService(IExtensionSubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
    }

    /// <summary>
    /// Creates a new subscription request from source to target extension.
    /// Records with status "Requested", reason, and expected_usage.
    /// </summary>
    public async Task<Guid> RequestSubscriptionAsync(
        string sourceExtensionId,
        string targetExtensionId,
        string? reason,
        string? expectedUsage,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceExtensionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetExtensionId);

        // Check if subscription already exists between source and target
        var existing = await _subscriptionRepository.GetBySourceAndTargetAsync(
            sourceExtensionId, targetExtensionId, cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Subscription already exists between '{sourceExtensionId}' and '{targetExtensionId}' with status '{existing.Status}'.");
        }

        var subscriptionId = Guid.NewGuid();
        var record = new ExtensionSubscriptionRecord(
            SubscriptionId: subscriptionId,
            SourceExtensionId: sourceExtensionId,
            TargetExtensionId: targetExtensionId,
            Status: SubscriptionStatus.Requested,
            Reason: reason,
            ExpectedUsage: expectedUsage,
            Conditions: null,
            DecidedBy: null,
            DecidedAt: null,
            ExpiresAt: null,
            RevokedAt: null,
            CreatedAt: DateTime.UtcNow);

        await _subscriptionRepository.AddAsync(record, cancellationToken);
        return subscriptionId;
    }

    /// <summary>
    /// Approve or reject a subscription request. Only target extension owner should call this.
    /// Transitions: Requested → Approved or Requested → Rejected.
    /// </summary>
    public async Task DecideAsync(
        Guid subscriptionId,
        string decision,
        Guid decidedBy,
        string? conditions,
        DateTime? expiresAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);

        var status = decision.ToLowerInvariant() switch
        {
            "approved" or "approve" => SubscriptionStatus.Approved,
            "rejected" or "reject" => SubscriptionStatus.Rejected,
            _ => throw new ArgumentException(
                $"Invalid decision: '{decision}'. Must be 'Approved' or 'Rejected'.", nameof(decision))
        };

        var existing = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Subscription '{subscriptionId}' not found.");
        }

        if (existing.Status != SubscriptionStatus.Requested)
        {
            throw new InvalidOperationException(
                $"Subscription '{subscriptionId}' cannot be decided. Current status is '{existing.Status}', expected 'Requested'.");
        }

        var updated = existing with
        {
            Status = status,
            DecidedBy = decidedBy,
            DecidedAt = DateTime.UtcNow,
            Conditions = conditions,
            ExpiresAt = expiresAt
        };

        await _subscriptionRepository.UpdateAsync(updated, cancellationToken);
    }

    /// <summary>
    /// Checks if a source extension has an active approved subscription to target.
    /// Active means: status = Approved AND (expires_at IS NULL OR expires_at > now).
    /// </summary>
    public async Task<bool> HasActiveSubscriptionAsync(
        string sourceExtensionId,
        string targetExtensionId,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetBySourceAndTargetAsync(
            sourceExtensionId, targetExtensionId, cancellationToken);

        if (subscription is null)
            return false;

        if (subscription.Status != SubscriptionStatus.Approved)
            return false;

        // Check expiration: if expires_at is set and in the past, subscription is inactive
        if (subscription.ExpiresAt.HasValue && subscription.ExpiresAt.Value <= DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Revoke an existing subscription. Sets status to Revoked with timestamp.
    /// </summary>
    public async Task RevokeAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var existing = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Subscription '{subscriptionId}' not found.");
        }

        var updated = existing with
        {
            Status = SubscriptionStatus.Revoked,
            RevokedAt = DateTime.UtcNow
        };

        await _subscriptionRepository.UpdateAsync(updated, cancellationToken);
    }
}
