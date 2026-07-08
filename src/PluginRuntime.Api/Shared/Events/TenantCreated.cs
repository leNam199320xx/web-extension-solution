using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Events;

/// <summary>
/// Published when a new tenant registers on the platform.
/// Subscribers: Billing module (creates Stripe customer).
/// </summary>
public sealed record TenantCreated(
    Guid EventId,
    DateTime OccurredAt,
    Guid TenantId,
    string Name,
    string ContactEmail,
    bool IsInternal) : IDomainEvent;
