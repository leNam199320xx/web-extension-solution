using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Gateway.Services;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Gateway.EventHandlers;

/// <summary>
/// Handles PackageCompositionChanged events by recalculating plugin access
/// for all tenants with active subscriptions to the affected package,
/// then publishing Redis notifications for each.
/// </summary>
public sealed class PackageCompositionChangedHandler : IDomainEventHandler<PackageCompositionChanged>
{
    private readonly IPluginAccessResolver _accessResolver;
    private readonly IGatewayNotificationService _notificationService;
    private readonly AppDbContext _db;
    private readonly ILogger<PackageCompositionChangedHandler> _logger;

    public PackageCompositionChangedHandler(
        IPluginAccessResolver accessResolver,
        IGatewayNotificationService notificationService,
        AppDbContext db,
        ILogger<PackageCompositionChangedHandler> logger)
    {
        _accessResolver = accessResolver;
        _notificationService = notificationService;
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(PackageCompositionChanged domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Handling PackageCompositionChanged for package {PackageId}: +{Added} / -{Removed} plugins",
            domainEvent.PackageId,
            domainEvent.AddedPluginIds.Count,
            domainEvent.RemovedPluginIds.Count);

        // Find all tenants with active subscriptions to this package
        var affectedTenantIds = await _db.PackageSubscriptions
            .Where(ps => ps.PackageId == domainEvent.PackageId && ps.Status == SubscriptionStatus.Active)
            .Select(ps => ps.TenantId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation(
            "Recalculating access for {Count} tenants affected by package {PackageId} composition change",
            affectedTenantIds.Count,
            domainEvent.PackageId);

        foreach (var tenantId in affectedTenantIds)
        {
            // Recalculate access
            await _accessResolver.RecalculateAccessAsync(tenantId, ct);

            // Get tenant version
            var tenant = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);

            var version = tenant?.Version ?? 0;

            // Get updated access set
            var accessiblePlugins = await _accessResolver.GetAccessiblePluginsAsync(tenantId, ct);

            // Publish notification
            await _notificationService.PublishAccessChangedAsync(
                tenantId,
                accessiblePlugins.ToList(),
                version,
                ct);
        }
    }
}
