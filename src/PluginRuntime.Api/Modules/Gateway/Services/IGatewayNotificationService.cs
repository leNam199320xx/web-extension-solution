using PluginRuntime.Api.Shared.Events;

namespace PluginRuntime.Api.Modules.Gateway.Services;

/// <summary>
/// Publishes cache-invalidation notifications to Redis pub/sub channels.
/// The external Public API Gateway subscribes to these channels to keep its cache in sync.
/// Implements retry logic: 3 retries with 5-second intervals, persists on exhaustion.
/// </summary>
public interface IGatewayNotificationService
{
    /// <summary>
    /// Publishes a plan-changed notification on the tenant:plan-changed channel.
    /// </summary>
    Task PublishPlanChangedAsync(PlanChanged evt, CancellationToken ct);

    /// <summary>
    /// Publishes a tenant-status-changed notification on the tenant:status-changed channel.
    /// </summary>
    Task PublishTenantStatusChangedAsync(TenantStatusChanged evt, CancellationToken ct);

    /// <summary>
    /// Publishes a key-revoked notification on the tenant:key-revoked channel.
    /// </summary>
    Task PublishKeyRevokedAsync(KeyRevoked evt, CancellationToken ct);

    /// <summary>
    /// Publishes an access-changed notification on the tenant:access-changed channel.
    /// </summary>
    Task PublishAccessChangedAsync(Guid tenantId, IReadOnlyList<Guid> pluginIds, long version, CancellationToken ct);
}
