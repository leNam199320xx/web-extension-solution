namespace PluginRuntime.Api.Shared.Interfaces;

/// <summary>
/// Dispatches domain events to registered handlers within the same process.
/// Handlers execute in-process; transactional handlers run within the caller's
/// DbContext transaction, fire-and-forget handlers run after transaction commit.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches an event to all registered handlers.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type.</typeparam>
    /// <param name="domainEvent">The event instance to dispatch.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken ct)
        where TEvent : IDomainEvent;
}
