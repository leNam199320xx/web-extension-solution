namespace PluginRuntime.Api.Shared.Interfaces;

/// <summary>
/// Marker interface for all domain events.
/// Every domain event carries a unique identifier and a timestamp.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Unique identifier for this event instance.</summary>
    Guid EventId { get; }

    /// <summary>UTC timestamp when this event occurred.</summary>
    DateTime OccurredAt { get; }
}
