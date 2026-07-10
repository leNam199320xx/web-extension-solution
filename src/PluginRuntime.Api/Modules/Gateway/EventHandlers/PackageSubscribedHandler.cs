using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Gateway.Services;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Gateway.EventHandlers;

/// <summary>
/// Handles PackageSubscribed events by recalculating the tenant's plugin access
/// and publishing a Redis notification with the updated access set.
/// </summary>
public sealed class PackageSubscribedHandler : IDomainEventHandler<PackageSubscribed>
{
    private readonly IPluginAccessResolver _accessResolver;
    private readonly IGatewayNotificationService _notificationService;
    private readonly AppDbContext _db;
    private readonly ILogger<PackageSubscribedHandler> _logger;

    public PackageSubscribedHandler(
        IPluginAccessResolver accessResolver,
        IGatewayNotificationService notificationService,
        AppDbContext db,
        ILogger<PackageSubscribedHandler> logger)
    {
        _accessResolver = accessResolver;
        _notificationService = notificationService;
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(PackageSubscribed domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Handling PackageSubscribed for tenant {TenantId}, package {PackageId}",
            domainEvent.TenantId,
            domainEvent.PackageId);

        // Recalculate plugin access
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
