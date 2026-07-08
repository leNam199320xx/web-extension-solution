using PluginRuntime.Api.Modules.Subscriptions.DTOs;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Service for managing tenant subscriptions to plugin packages.
/// Handles subscribe, unsubscribe, and listing operations with plan limit enforcement.
/// </summary>
public interface IPackageSubscriptionService
{
    /// <summary>
    /// Subscribes a tenant to a plugin package.
    /// Enforces plan limits and creates Stripe subscription item if applicable.
    /// </summary>
    Task<PackageSubscriptionDto> SubscribeAsync(Guid tenantId, Guid packageId, CancellationToken ct);

    /// <summary>
    /// Unsubscribes a tenant from a plugin package.
    /// Cancels at period end for billing continuity.
    /// </summary>
    Task UnsubscribeAsync(Guid tenantId, Guid packageId, CancellationToken ct);

    /// <summary>
    /// Lists all active package subscriptions for a tenant.
    /// </summary>
    Task<IReadOnlyList<PackageSubscriptionDto>> ListActiveAsync(Guid tenantId, CancellationToken ct);
}
