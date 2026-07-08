using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Events;

/// <summary>
/// Published when a tenant subscribes to a plugin package.
/// Subscribers: Gateway module (updates plugin_access, publishes Redis notification).
/// </summary>
public sealed record PackageSubscribed(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid PackageId,
    Guid SubscriptionId,
    IReadOnlyList<Guid> PluginIds) : IDomainEvent;
