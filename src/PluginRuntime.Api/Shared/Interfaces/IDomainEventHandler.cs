namespace PluginRuntime.Api.Shared.Interfaces;

/// <summary>
/// Handler for a specific domain event type.
/// Implement this interface to react to domain events dispatched by other modules.
/// </summary>
/// <typeparam name="TEvent">The domain event type this handler processes.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event asynchronously.
    /// </summary>
    /// <param name="domainEvent">The event to handle.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);
}
