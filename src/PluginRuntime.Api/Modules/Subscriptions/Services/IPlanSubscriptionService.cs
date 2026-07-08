using PluginRuntime.Api.Modules.Subscriptions.DTOs;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Manages tenant plan subscriptions including upgrades and downgrades.
/// </summary>
public interface IPlanSubscriptionService
{
    /// <summary>
    /// Changes the tenant's plan. Upgrades apply immediately with proration;
    /// downgrades are scheduled for the next billing period.
    /// </summary>
    Task<PlanChangeResult> ChangePlanAsync(Guid tenantId, PlanChangeRequest request, CancellationToken ct);

    /// <summary>
    /// Returns the tenant's current subscription details.
    /// </summary>
    Task<CurrentSubscriptionDto> GetCurrentAsync(Guid tenantId, CancellationToken ct);
}
