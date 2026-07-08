using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Billing.Services;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Manages plan subscription changes (upgrades/downgrades) for tenants.
/// Upgrades apply immediately with Stripe proration.
/// Downgrades are scheduled for the next billing period.
/// </summary>
public sealed class PlanSubscriptionService : IPlanSubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IStripeService _stripeService;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<PlanSubscriptionService> _logger;

    public PlanSubscriptionService(
        AppDbContext db,
        IStripeService stripeService,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PlanSubscriptionService> logger)
    {
        _db = db;
        _stripeService = stripeService;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PlanChangeResult> ChangePlanAsync(Guid tenantId, PlanChangeRequest request, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");

        var currentPlan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == tenant.PlanId, ct)
            ?? throw new DomainException("Current plan not found. Platform configuration error.");

        var newPlan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == request.NewPlanId, ct)
            ?? throw new DomainException($"Plan with ID '{request.NewPlanId}' not found.");

        if (newPlan.PlanId == currentPlan.PlanId)
            throw new DomainException("Already on this plan.");

        PlanChangeResult result;

        if (newPlan.MonthlyPrice > currentPlan.MonthlyPrice)
        {
            // UPGRADE: apply immediately with proration
            result = await HandleUpgradeAsync(tenant, currentPlan, newPlan, ct);
        }
        else
        {
            // DOWNGRADE: schedule for next billing period
            await ValidateDowngradeLimitsAsync(tenantId, newPlan, ct);
            result = await HandleDowngradeAsync(tenant, currentPlan, newPlan, ct);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Plan change {Type} for tenant {TenantId}: {OldPlan} → {NewPlan}, effective {EffectiveAt}",
            result.Type, tenantId, currentPlan.Name, newPlan.Name, result.EffectiveAt);

        return result;
    }

    /// <inheritdoc />
    public async Task<CurrentSubscriptionDto> GetCurrentAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");

        var plan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == tenant.PlanId, ct)
            ?? throw new DomainException("Associated plan not found. Platform configuration error.");

        return new CurrentSubscriptionDto(
            PlanId: plan.PlanId,
            PlanName: plan.Name,
            RateLimit: plan.RateLimit,
            DailyQuota: plan.DailyQuota,
            MonthlyPrice: plan.MonthlyPrice,
            PendingPlanId: tenant.PendingPlanId);
    }

    private async Task<PlanChangeResult> HandleUpgradeAsync(
        Tenant tenant, Plan currentPlan, Plan newPlan, CancellationToken ct)
    {
        // Call Stripe to update subscription with proration
        if (!string.IsNullOrEmpty(tenant.StripeSubscriptionId) && !string.IsNullOrEmpty(newPlan.StripePriceId))
        {
            await _stripeService.UpdateSubscriptionAsync(
                tenant.StripeSubscriptionId,
                newPlan.StripePriceId,
                prorate: true,
                ct);
        }

        // Apply plan change immediately
        var oldPlanId = tenant.PlanId;
        tenant.AssignPlan(newPlan.PlanId);

        // Dispatch PlanChanged event with new limits
        await _eventDispatcher.DispatchAsync(new PlanChanged(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenant.TenantId,
            OldPlanId: oldPlanId,
            NewPlanId: newPlan.PlanId,
            NewRateLimit: newPlan.RateLimit,
            NewDailyQuota: newPlan.DailyQuota,
            Version: tenant.Version), ct);

        return new PlanChangeResult(
            Type: "Upgrade",
            EffectiveAt: DateTime.UtcNow,
            ProratedAmount: null);
    }

    private async Task<PlanChangeResult> HandleDowngradeAsync(
        Tenant tenant, Plan currentPlan, Plan newPlan, CancellationToken ct)
    {
        // Schedule for next billing period
        tenant.SetPendingPlan(newPlan.PlanId);

        // Notify Stripe to cancel current price at period end (if applicable)
        if (!string.IsNullOrEmpty(tenant.StripeSubscriptionId) && !string.IsNullOrEmpty(newPlan.StripePriceId))
        {
            await _stripeService.UpdateSubscriptionAsync(
                tenant.StripeSubscriptionId,
                newPlan.StripePriceId,
                prorate: false,
                ct);
        }

        // Calculate next billing date (approximate: 30 days from now)
        var nextBillingDate = DateTime.UtcNow.AddDays(30);

        return new PlanChangeResult(
            Type: "Downgrade",
            EffectiveAt: nextBillingDate,
            ProratedAmount: null);
    }

    /// <summary>
    /// Validates that the tenant's active resources do not exceed the new plan's limits.
    /// Prevents downgrade if current usage would violate the new plan's constraints.
    /// </summary>
    private async Task ValidateDowngradeLimitsAsync(Guid tenantId, Plan newPlan, CancellationToken ct)
    {
        // Check active API keys against new plan limit
        if (newPlan.MaxApiKeys.HasValue)
        {
            var activeKeyCount = await _db.ApiKeys
                .CountAsync(k => k.TenantId == tenantId && k.Status == ApiKeyStatus.Active, ct);

            if (activeKeyCount > newPlan.MaxApiKeys.Value)
            {
                throw new SubscriptionLimitException(
                    "UA-PLAN-002",
                    $"Cannot downgrade: {activeKeyCount} active API keys exceed new plan limit of {newPlan.MaxApiKeys.Value}. " +
                    "Revoke excess keys before downgrading.");
            }
        }

        // Check active package subscriptions against new plan limit
        if (newPlan.MaxPackageSubscriptions.HasValue)
        {
            var activeSubCount = await _db.Set<PackageSubscription>()
                .CountAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, ct);

            if (activeSubCount > newPlan.MaxPackageSubscriptions.Value)
            {
                throw new SubscriptionLimitException(
                    "UA-PLAN-002",
                    $"Cannot downgrade: {activeSubCount} active package subscriptions exceed new plan limit of {newPlan.MaxPackageSubscriptions.Value}. " +
                    "Cancel excess subscriptions before downgrading.");
            }
        }
    }
}
