using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Events;

/// <summary>
/// Published when a tenant's status transitions (e.g., active → suspended).
/// Subscribers: Gateway module (publishes Redis notification for access revocation).
/// </summary>
public sealed record TenantStatusChanged(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    string PreviousStatus,
    string NewStatus,
    string ActorId,
    string Reason) : IDomainEvent;
