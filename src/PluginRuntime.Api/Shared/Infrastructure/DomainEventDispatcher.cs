using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Infrastructure;

/// <summary>
/// Dispatches domain events to all registered handlers via the DI container.
/// Each handler is resolved from a new scope and invoked independently.
/// Errors in individual handlers are logged but do not prevent other handlers from executing.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(domainEvent, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error handling domain event {EventType} (EventId={EventId}) in handler {HandlerType}",
                    typeof(TEvent).Name,
                    domainEvent.EventId,
                    handler.GetType().Name);
            }
        }
    }
}
