using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Events;

/// <summary>
/// Published when a tenant cancels a plugin package subscription.
/// Subscribers: Gateway module (revokes plugin access, publishes Redis notification).
/// </summary>
public sealed record PackageUnsubscribed(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid PackageId,
    Guid SubscriptionId,
    IReadOnlyList<Guid> PluginIds) : IDomainEvent;
