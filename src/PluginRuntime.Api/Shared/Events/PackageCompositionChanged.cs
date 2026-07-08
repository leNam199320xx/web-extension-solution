using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Events;

/// <summary>
/// Published when plugins are added to or removed from a plugin package.
/// Subscribers: Gateway module (recalculates affected tenants' plugin_access sets).
/// </summary>
public sealed record PackageCompositionChanged(
    Guid EventId,
    DateTime OccurredAt,
    Guid PackageId,
    IReadOnlyList<Guid> AddedPluginIds,
    IReadOnlyList<Guid> RemovedPluginIds) : IDomainEvent;
