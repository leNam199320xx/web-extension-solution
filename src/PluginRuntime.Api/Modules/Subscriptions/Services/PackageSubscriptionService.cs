using PluginRuntime.Api.Modules.Billing.Services;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Manages tenant subscriptions to plugin packages using IRepository.
/// </summary>
public sealed class PackageSubscriptionService : IPackageSubscriptionService
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<Plan> _plans;
    private readonly IRepository<PackageSubscription> _subscriptions;
    private readonly IRepository<PluginPackage> _packages;
    private readonly IStripeService _stripeService;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PackageSubscriptionService(
        IRepository<Tenant> tenants,
        IRepository<Plan> plans,
        IRepository<PackageSubscription> subscriptions,
        IRepository<PluginPackage> packages,
        IStripeService stripeService,
        IDomainEventDispatcher eventDispatcher)
    {
        _tenants = tenants;
        _plans = plans;
        _subscriptions = subscriptions;
        _packages = packages;
        _stripeService = stripeService;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PackageSubscriptionDto> SubscribeAsync(Guid tenantId, Guid packageId, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant with ID '{tenantId}' not found.");

        var plan = await _plans.GetByIdAsync(tenant.PlanId, ct)
            ?? throw new KeyNotFoundException($"Plan with ID '{tenant.PlanId}' not found.");

        if (plan.MaxPackageSubscriptions == 0)
            throw new SubscriptionLimitException("UA-SUB-003", "Free plan cannot subscribe to packages");

        var activeCount = await _subscriptions.CountAsync(
            s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, ct);

        if (plan.MaxPackageSubscriptions is not null && activeCount >= plan.MaxPackageSubscriptions)
            throw new SubscriptionLimitException("UA-SUB-001", "Max package subscriptions reached");

        var existingSubs = await _subscriptions.FindAsync(
            s => s.TenantId == tenantId && s.PackageId == packageId && s.Status == SubscriptionStatus.Active, ct);
        if (existingSubs.Count > 0) throw new DuplicateSubscriptionException();

        // Load package with plugins via Query
        var package = _packages.Query()
            .FirstOrDefault(p => p.PackageId == packageId)
            ?? throw new KeyNotFoundException($"Package with ID '{packageId}' not found.");

        if (package.Status != PackageStatus.Active)
            throw new PackageValidationException($"Package '{packageId}' is not active.");

        var subscription = PackageSubscription.Create(tenantId, packageId, stripeItemId: null);

        if (!string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId) && !string.IsNullOrWhiteSpace(package.StripePriceId))
        {
            var stripeItemId = await _stripeService.AddSubscriptionItemAsync(tenant.StripeSubscriptionId, package.StripePriceId, ct);
            subscription.SetStripeItemId(stripeItemId);
        }

        await _subscriptions.AddAsync(subscription, ct);
        await _subscriptions.SaveChangesAsync(ct);

        var pluginIds = package.Plugins.Select(p => p.PluginId).ToList();
        await _eventDispatcher.DispatchAsync(new PackageSubscribed(
            Guid.NewGuid(), DateTime.UtcNow, tenantId, packageId, subscription.SubscriptionId, pluginIds), ct);

        return PackageSubscriptionDto.FromEntity(subscription);
    }

    public async Task UnsubscribeAsync(Guid tenantId, Guid packageId, CancellationToken ct)
    {
        var subs = await _subscriptions.FindAsync(
            s => s.TenantId == tenantId && s.PackageId == packageId && s.Status == SubscriptionStatus.Active, ct);

        var subscription = subs.FirstOrDefault()
            ?? throw new KeyNotFoundException($"No active subscription for tenant '{tenantId}' and package '{packageId}'.");

        if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionItemId))
            await _stripeService.CancelSubscriptionItemAsync(subscription.StripeSubscriptionItemId, atPeriodEnd: true, ct);

        subscription.Cancel();
        await _subscriptions.UpdateAsync(subscription, ct);
        await _subscriptions.SaveChangesAsync(ct);

        var package = _packages.Query().FirstOrDefault(p => p.PackageId == packageId);
        var pluginIds = package?.Plugins.Select(p => p.PluginId).ToList() ?? [];

        await _eventDispatcher.DispatchAsync(new PackageUnsubscribed(
            Guid.NewGuid(), DateTime.UtcNow, tenantId, packageId, subscription.SubscriptionId, pluginIds), ct);
    }

    public async Task<IReadOnlyList<PackageSubscriptionDto>> ListActiveAsync(Guid tenantId, CancellationToken ct)
    {
        var subs = await _subscriptions.FindAsync(
            s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, ct);
        return subs.OrderBy(s => s.StartDate).Select(PackageSubscriptionDto.FromEntity).ToList();
    }
}
