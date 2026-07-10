using PluginRuntime.Api.Modules.Gateway.Domain;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Gateway.Services;

/// <summary>
/// Resolves plugin access using IRepository for provider-agnostic persistence.
/// </summary>
public sealed class PluginAccessResolver : IPluginAccessResolver
{
    private readonly IRepository<PluginAccess> _accessRepo;
    private readonly IRepository<PackageSubscription> _subscriptions;
    private readonly IRepository<PluginPackage> _packages;
    private readonly IRepository<PackagePlugin> _packagePlugins;
    private readonly ILogger<PluginAccessResolver> _logger;

    public PluginAccessResolver(
        IRepository<PluginAccess> accessRepo,
        IRepository<PackageSubscription> subscriptions,
        IRepository<PluginPackage> packages,
        IRepository<PackagePlugin> packagePlugins,
        ILogger<PluginAccessResolver> logger)
    {
        _accessRepo = accessRepo;
        _subscriptions = subscriptions;
        _packages = packages;
        _packagePlugins = packagePlugins;
        _logger = logger;
    }

    public async Task<IReadOnlySet<Guid>> GetAccessiblePluginsAsync(Guid tenantId, CancellationToken ct)
    {
        var entries = await _accessRepo.FindAsync(pa => pa.TenantId == tenantId, ct);
        return entries.Select(pa => pa.PluginId).ToHashSet();
    }

    public async Task RecalculateAccessAsync(Guid tenantId, CancellationToken ct)
    {
        _logger.LogInformation("Recalculating plugin access for tenant {TenantId}", tenantId);

        // 1. Get package subscription plugins for this tenant
        var activeSubs = await _subscriptions.FindAsync(
            ps => ps.TenantId == tenantId && ps.Status == SubscriptionStatus.Active, ct);

        var activePackageIds = activeSubs.Select(s => s.PackageId).ToHashSet();

        var allPackagePlugins = await _packagePlugins.GetAllAsync(ct);
        var packagePluginEntries = allPackagePlugins
            .Where(pp => activePackageIds.Contains(pp.PackageId))
            .ToList();

        // 2. Remove existing access entries
        var existing = await _accessRepo.FindAsync(pa => pa.TenantId == tenantId, ct);
        await _accessRepo.RemoveRangeAsync(existing, ct);

        // 3. Create new access entries from package plugins
        var accessEntries = packagePluginEntries
            .Select(pp => PluginAccess.Create(tenantId, pp.PluginId, AccessSource.Package, pp.PackageId))
            .ToList();

        if (accessEntries.Count > 0)
            await _accessRepo.AddRangeAsync(accessEntries, ct);

        await _accessRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Plugin access recalculated for tenant {TenantId}: {Count} plugins", tenantId, accessEntries.Count);
    }

    public async Task RecalculateForPackageAsync(Guid packageId, CancellationToken ct)
    {
        var affectedSubs = await _subscriptions.FindAsync(
            ps => ps.PackageId == packageId && ps.Status == SubscriptionStatus.Active, ct);

        var affectedTenantIds = affectedSubs.Select(s => s.TenantId).Distinct().ToList();

        foreach (var tenantId in affectedTenantIds)
        {
            await RecalculateAccessAsync(tenantId, ct);
        }
    }
}
