using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Billing.Services;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Service managing tenant subscriptions to plugin packages.
/// Enforces plan limits, integrates with Stripe billing, and dispatches domain events.
/// </summary>
public sealed class PackageSubscriptionService : IPackageSubscriptionService
{
    private readonly AppDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PackageSubscriptionService(
        AppDbContext dbContext,
        IStripeService stripeService,
        IDomainEventDispatcher eventDispatcher)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PackageSubscriptionDto> SubscribeAsync(Guid tenantId, Guid packageId, CancellationToken ct)
    {
        // 1. Load tenant + plan
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant with ID '{tenantId}' not found.");

        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.PlanId == tenant.PlanId, ct)
            ?? throw new KeyNotFoundException($"Plan with ID '{tenant.PlanId}' not found.");

        // 2. Free plan cannot subscribe (max_package_subscriptions = 0)
        if (plan.MaxPackageSubscriptions == 0)
        {
            throw new SubscriptionLimitException(
                "UA-SUB-003",
                "Free plan cannot subscribe to packages");
        }

        // 3. Count active subscriptions for tenant
        var activeCount = await _dbContext.PackageSubscriptions
            .CountAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, ct);

        // 4. Check if limit exceeded (null = unlimited)
        if (plan.MaxPackageSubscriptions is not null && activeCount >= plan.MaxPackageSubscriptions)
        {
            throw new SubscriptionLimitException(
                "UA-SUB-001",
                "Max package subscriptions reached");
        }

        // 5. Check for duplicate subscription (active sub for same tenant+package)
        var existingSub = await _dbContext.PackageSubscriptions
            .AnyAsync(s => s.TenantId == tenantId
                        && s.PackageId == packageId
                        && s.Status == SubscriptionStatus.Active, ct);

        if (existingSub)
        {
            throw new DuplicateSubscriptionException();
        }

        // 6. Verify package exists and is Active
        var package = await _dbContext.PluginPackages
            .Include(p => p.Plugins)
            .FirstOrDefaultAsync(p => p.PackageId == packageId, ct)
            ?? throw new KeyNotFoundException($"Package with ID '{packageId}' not found.");

        if (package.Status != PackageStatus.Active)
        {
            throw new PackageValidationException(
                $"Package '{packageId}' is not active and cannot be subscribed to.");
        }

        // 7. Create PackageSubscription entity
        var subscription = PackageSubscription.Create(tenantId, packageId, stripeItemId: null);

        // 8. If tenant has StripeSubscriptionId, create Stripe subscription item
        if (!string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId) &&
            !string.IsNullOrWhiteSpace(package.StripePriceId))
        {
            var stripeItemId = await _stripeService.AddSubscriptionItemAsync(
                tenant.StripeSubscriptionId,
                package.StripePriceId,
                ct);

            subscription.SetStripeItemId(stripeItemId);
        }

        // 9. Save
        _dbContext.PackageSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(ct);

        // Dispatch PackageSubscribed event with plugin IDs from the package
        var pluginIds = package.Plugins.Select(p => p.PluginId).ToList();

        var domainEvent = new PackageSubscribed(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenantId,
            PackageId: packageId,
            SubscriptionId: subscription.SubscriptionId,
            PluginIds: pluginIds);

        await _eventDispatcher.DispatchAsync(domainEvent, ct);

        // 10. Return DTO
        return PackageSubscriptionDto.FromEntity(subscription);
    }

    public async Task UnsubscribeAsync(Guid tenantId, Guid packageId, CancellationToken ct)
    {
        // 1. Load active subscription for tenant+package
        var subscription = await _dbContext.PackageSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId
                                   && s.PackageId == packageId
                                   && s.Status == SubscriptionStatus.Active, ct)
            ?? throw new KeyNotFoundException(
                $"No active subscription found for tenant '{tenantId}' and package '{packageId}'.");

        // 2. If has StripeSubscriptionItemId, cancel at period end
        if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionItemId))
        {
            await _stripeService.CancelSubscriptionItemAsync(
                subscription.StripeSubscriptionItemId,
                atPeriodEnd: true,
                ct);
        }

        // 3. Cancel subscription
        subscription.Cancel();
        await _dbContext.SaveChangesAsync(ct);

        // 4. Dispatch PackageUnsubscribed event with plugin IDs from the package
        var package = await _dbContext.PluginPackages
            .Include(p => p.Plugins)
            .FirstOrDefaultAsync(p => p.PackageId == packageId, ct);

        var pluginIds = package?.Plugins.Select(p => p.PluginId).ToList()
                        ?? new List<Guid>();

        var domainEvent = new PackageUnsubscribed(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenantId,
            PackageId: packageId,
            SubscriptionId: subscription.SubscriptionId,
            PluginIds: pluginIds);

        await _eventDispatcher.DispatchAsync(domainEvent, ct);
    }

    public async Task<IReadOnlyList<PackageSubscriptionDto>> ListActiveAsync(Guid tenantId, CancellationToken ct)
    {
        var subscriptions = await _dbContext.PackageSubscriptions
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .OrderBy(s => s.StartDate)
            .ToListAsync(ct);

        return subscriptions.Select(PackageSubscriptionDto.FromEntity).ToList();
    }
}
