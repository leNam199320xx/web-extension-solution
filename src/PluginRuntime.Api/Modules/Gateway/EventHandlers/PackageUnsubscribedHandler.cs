using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Gateway.Services;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Gateway.EventHandlers;

/// <summary>
/// Handles PackageUnsubscribed events by recalculating the tenant's plugin access
/// and publishing a Redis notification with the updated (reduced) access set.
/// </summary>
public sealed class PackageUnsubscribedHandler : IDomainEventHandler<PackageUnsubscribed>
{
    private readonly IPluginAccessResolver _accessResolver;
    private readonly IGatewayNotificationService _notificationService;
    private readonly AppDbContext _db;
    private readonly ILogger<PackageUnsubscribedHandler> _logger;

    public PackageUnsubscribedHandler(
        IPluginAccessResolver accessResolver,
        IGatewayNotificationService notificationService,
        AppDbContext db,
        ILogger<PackageUnsubscribedHandler> logger)
    {
        _accessResolver = accessResolver;
        _notificationService = notificationService;
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(PackageUnsubscribed domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Handling PackageUnsubscribed for tenant {TenantId}, package {PackageId}",
            domainEvent.TenantId,
            domainEvent.PackageId);

        // Recalculate plugin access (removes access from unsubscribed package)
        await _accessResolver.RecalculateAccessAsync(domainEvent.TenantId, ct);

        // Get tenant version for notification ordering
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == domainEvent.TenantId, ct);

        var version = tenant?.Version ?? 0;

        // Get updated access set
        var accessiblePlugins = await _accessResolver.GetAccessiblePluginsAsync(domainEvent.TenantId, ct);

        // Publish notification
        await _notificationService.PublishAccessChangedAsync(
            domainEvent.TenantId,
            accessiblePlugins.ToList(),
            version,
            ct);
    }
}
