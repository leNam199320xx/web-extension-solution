using PluginRuntime.Api.Modules.Billing.Services;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Manages plan subscription changes using IRepository.
/// </summary>
public sealed class PlanSubscriptionService : IPlanSubscriptionService
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<Plan> _plans;
    private readonly IRepository<ApiKey> _apiKeys;
    private readonly IRepository<PackageSubscription> _packageSubs;
    private readonly IStripeService _stripeService;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<PlanSubscriptionService> _logger;

    public PlanSubscriptionService(
        IRepository<Tenant> tenants,
        IRepository<Plan> plans,
        IRepository<ApiKey> apiKeys,
        IRepository<PackageSubscription> packageSubs,
        IStripeService stripeService,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PlanSubscriptionService> logger)
    {
        _tenants = tenants;
        _plans = plans;
        _apiKeys = apiKeys;
        _packageSubs = packageSubs;
        _stripeService = stripeService;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<PlanChangeResult> ChangePlanAsync(Guid tenantId, PlanChangeRequest request, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct)
            ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");

        var currentPlan = await _plans.GetByIdAsync(tenant.PlanId, ct)
            ?? throw new DomainException("Current plan not found.");

        var newPlan = await _plans.GetByIdAsync(request.NewPlanId, ct)
            ?? throw new DomainException($"Plan with ID '{request.NewPlanId}' not found.");

        if (newPlan.PlanId == currentPlan.PlanId)
            throw new DomainException("Already on this plan.");

        PlanChangeResult result;

        if (newPlan.MonthlyPrice > currentPlan.MonthlyPrice)
        {
            result = await HandleUpgradeAsync(tenant, currentPlan, newPlan, ct);
        }
        else
        {
            await ValidateDowngradeLimitsAsync(tenantId, newPlan, ct);
            result = await HandleDowngradeAsync(tenant, newPlan, ct);
        }

        await _tenants.UpdateAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);

        _logger.LogInformation("Plan change {Type} for tenant {TenantId}: effective {EffectiveAt}",
            result.Type, tenantId, result.EffectiveAt);

        return result;
    }

    public async Task<CurrentSubscriptionDto> GetCurrentAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct)
            ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");

        var plan = await _plans.GetByIdAsync(tenant.PlanId, ct)
            ?? throw new DomainException("Associated plan not found.");

        return new CurrentSubscriptionDto(plan.PlanId, plan.Name, plan.RateLimit, plan.DailyQuota, plan.MonthlyPrice, tenant.PendingPlanId);
    }

    private async Task<PlanChangeResult> HandleUpgradeAsync(Tenant tenant, Plan currentPlan, Plan newPlan, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(tenant.StripeSubscriptionId) && !string.IsNullOrEmpty(newPlan.StripePriceId))
            await _stripeService.UpdateSubscriptionAsync(tenant.StripeSubscriptionId, newPlan.StripePriceId, prorate: true, ct);

        var oldPlanId = tenant.PlanId;
        tenant.AssignPlan(newPlan.PlanId);

        await _eventDispatcher.DispatchAsync(new PlanChanged(
            Guid.NewGuid(), DateTime.UtcNow, tenant.TenantId, oldPlanId, newPlan.PlanId,
            newPlan.RateLimit, newPlan.DailyQuota, tenant.Version), ct);

        return new PlanChangeResult("Upgrade", DateTime.UtcNow, null);
    }

    private async Task<PlanChangeResult> HandleDowngradeAsync(Tenant tenant, Plan newPlan, CancellationToken ct)
    {
        tenant.SetPendingPlan(newPlan.PlanId);

        if (!string.IsNullOrEmpty(tenant.StripeSubscriptionId) && !string.IsNullOrEmpty(newPlan.StripePriceId))
            await _stripeService.UpdateSubscriptionAsync(tenant.StripeSubscriptionId, newPlan.StripePriceId, prorate: false, ct);

        return new PlanChangeResult("Downgrade", DateTime.UtcNow.AddDays(30), null);
    }

    private async Task ValidateDowngradeLimitsAsync(Guid tenantId, Plan newPlan, CancellationToken ct)
    {
        if (newPlan.MaxApiKeys.HasValue)
        {
            var activeKeyCount = await _apiKeys.CountAsync(k => k.TenantId == tenantId && k.Status == ApiKeyStatus.Active, ct);
            if (activeKeyCount > newPlan.MaxApiKeys.Value)
                throw new SubscriptionLimitException("UA-PLAN-002",
                    $"Cannot downgrade: {activeKeyCount} active API keys exceed limit of {newPlan.MaxApiKeys.Value}.");
        }

        if (newPlan.MaxPackageSubscriptions.HasValue)
        {
            var activeSubCount = await _packageSubs.CountAsync(
                s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, ct);
            if (activeSubCount > newPlan.MaxPackageSubscriptions.Value)
                throw new SubscriptionLimitException("UA-PLAN-002",
                    $"Cannot downgrade: {activeSubCount} package subscriptions exceed limit of {newPlan.MaxPackageSubscriptions.Value}.");
        }
    }
}
