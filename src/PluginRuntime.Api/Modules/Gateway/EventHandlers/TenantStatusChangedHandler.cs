using PluginRuntime.Api.Modules.Gateway.Services;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Gateway.EventHandlers;

/// <summary>
/// Handles TenantStatusChanged events by publishing a Redis notification
/// so the Public API Gateway can suspend or reactivate access.
/// </summary>
public sealed class TenantStatusChangedHandler : IDomainEventHandler<TenantStatusChanged>
{
    private readonly IGatewayNotificationService _notificationService;
    private readonly ILogger<TenantStatusChangedHandler> _logger;

    public TenantStatusChangedHandler(
        IGatewayNotificationService notificationService,
        ILogger<TenantStatusChangedHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(TenantStatusChanged domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Handling TenantStatusChanged for tenant {TenantId}: {Previous} → {New}",
            domainEvent.TenantId,
            domainEvent.PreviousStatus,
            domainEvent.NewStatus);

        await _notificationService.PublishTenantStatusChangedAsync(domainEvent, ct);
    }
}
