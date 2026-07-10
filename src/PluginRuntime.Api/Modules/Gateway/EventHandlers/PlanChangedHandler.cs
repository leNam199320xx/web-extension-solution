using PluginRuntime.Api.Modules.Gateway.Services;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Gateway.EventHandlers;

/// <summary>
/// Handles PlanChanged events by publishing a Redis notification
/// with the new rate limit and daily quota for the Public API Gateway.
/// </summary>
public sealed class PlanChangedHandler : IDomainEventHandler<PlanChanged>
{
    private readonly IGatewayNotificationService _notificationService;
    private readonly ILogger<PlanChangedHandler> _logger;

    public PlanChangedHandler(
        IGatewayNotificationService notificationService,
        ILogger<PlanChangedHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(PlanChanged domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Handling PlanChanged for tenant {TenantId}: {OldPlanId} → {NewPlanId} (v{Version})",
            domainEvent.TenantId,
            domainEvent.OldPlanId,
            domainEvent.NewPlanId,
            domainEvent.Version);

        await _notificationService.PublishPlanChangedAsync(domainEvent, ct);
    }
}
