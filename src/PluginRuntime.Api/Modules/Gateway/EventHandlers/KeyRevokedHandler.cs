using PluginRuntime.Api.Modules.Gateway.Services;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Gateway.EventHandlers;

/// <summary>
/// Handles KeyRevoked events by publishing a Redis notification
/// so the Public API Gateway can invalidate the cached key.
/// </summary>
public sealed class KeyRevokedHandler : IDomainEventHandler<KeyRevoked>
{
    private readonly IGatewayNotificationService _notificationService;
    private readonly ILogger<KeyRevokedHandler> _logger;

    public KeyRevokedHandler(
        IGatewayNotificationService notificationService,
        ILogger<KeyRevokedHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(KeyRevoked domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Handling KeyRevoked for tenant {TenantId}, key {KeyId} (v{Version})",
            domainEvent.TenantId,
            domainEvent.KeyId,
            domainEvent.Version);

        await _notificationService.PublishKeyRevokedAsync(domainEvent, ct);
    }
}
