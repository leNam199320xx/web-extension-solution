using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Events;

/// <summary>
/// Published when a tenant's plan change takes effect.
/// Subscribers: Gateway module (publishes Redis notification with new rate limits/quotas).
/// </summary>
public sealed record PlanChanged(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid OldPlanId,
    Guid NewPlanId,
    int? NewRateLimit,
    int? NewDailyQuota,
    long Version) : IDomainEvent;
