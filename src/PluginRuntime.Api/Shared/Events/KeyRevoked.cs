using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Events;

/// <summary>
/// Published when an API key is revoked.
/// Subscribers: Gateway module (publishes Redis cache-invalidation event).
/// </summary>
public sealed record KeyRevoked(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    Guid KeyId,
    string KeyHash,
    long Version) : IDomainEvent;
