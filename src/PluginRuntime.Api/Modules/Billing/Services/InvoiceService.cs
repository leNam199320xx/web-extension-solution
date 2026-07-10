using PluginRuntime.Api.Modules.Billing.Domain;
using PluginRuntime.Api.Modules.Billing.DTOs;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Generates consolidated monthly invoices using IRepository for provider-agnostic persistence.
/// </summary>
public sealed class InvoiceService : IInvoiceService
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<Plan> _plans;
    private readonly IRepository<Invoice> _invoices;
    private readonly IRepository<UsageAggregate> _usageAggregates;
    private readonly IRepository<PackageSubscription> _packageSubscriptions;
    private readonly IRepository<PluginPackage> _pluginPackages;
    private readonly IStripeService _stripeService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IRepository<Tenant> tenants,
        IRepository<Plan> plans,
        IRepository<Invoice> invoices,
        IRepository<UsageAggregate> usageAggregates,
        IRepository<PackageSubscription> packageSubscriptions,
        IRepository<PluginPackage> pluginPackages,
        IStripeService stripeService,
        ILogger<InvoiceService> logger)
    {
        _tenants = tenants;
        _plans = plans;
        _invoices = invoices;
        _usageAggregates = usageAggregates;
        _packageSubscriptions = packageSubscriptions;
        _pluginPackages = pluginPackages;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task GenerateMonthlyInvoicesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var billingPeriodStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var billingPeriodEnd = billingPeriodStart.AddMonths(1).AddDays(-1);

        _logger.LogInformation("Generating monthly invoices for {Start} to {End}", billingPeriodStart, billingPeriodEnd);

        var allTenants = await _tenants.GetAllAsync(ct);
        var allPlans = await _plans.GetAllAsync(ct);
        var planLookup = allPlans.ToDictionary(p => p.PlanId);

        var billableTenants = allTenants
            .Where(t => planLookup.TryGetValue(t.PlanId, out var plan) && plan.IsBillable)
            .ToList();

        foreach (var tenant in billableTenants)
        {
            try
            {
                var plan = planLookup[tenant.PlanId];
                await GenerateInvoiceForTenantAsync(
                    tenant.TenantId, tenant.StripeCustomerId, plan.MonthlyPrice,
                    plan.DailyQuota, plan.OverageRatePer1k, billingPeriodStart, billingPeriodEnd, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate invoice for tenant {TenantId}", tenant.TenantId);
            }
        }
    }

    public async Task UpdateInvoiceStatusAsync(string stripeInvoiceId, InvoiceStatus newStatus, CancellationToken ct)
    {
        var invoices = await _invoices.FindAsync(i => i.StripeInvoiceId == stripeInvoiceId, ct);
        var invoice = invoices.FirstOrDefault();
        if (invoice is null) return;

        invoice.UpdateStatus(newStatus);
        await _invoices.UpdateAsync(invoice, ct);
        await _invoices.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<InvoiceDto>> GetTenantInvoicesAsync(Guid tenantId, PaginationParams paging, CancellationToken ct)
    {
        var normalized = paging.Normalize();
        var tenantInvoices = await _invoices.FindAsync(i => i.TenantId == tenantId, ct);

        var ordered = tenantInvoices.OrderByDescending(i => i.BillingPeriodStart).ToList();
        var totalCount = ordered.Count;
        var items = ordered.Skip(normalized.Skip).Take(normalized.Take).ToList();

        return new PagedResult<InvoiceDto>
        {
            Items = items.Select(InvoiceDto.FromEntity).ToList(),
            Page = normalized.Page,
            PageSize = normalized.PageSize,
            TotalCount = totalCount
        };
    }

    private async Task GenerateInvoiceForTenantAsync(
        Guid tenantId, string? stripeCustomerId, decimal baseAmount,
        int? dailyQuota, decimal? overageRatePer1k,
        DateOnly billingPeriodStart, DateOnly billingPeriodEnd, CancellationToken ct)
    {
        // Usage aggregates for this tenant in the billing period
        var usageAggregates = await _usageAggregates.FindAsync(
            u => u.TenantId == tenantId && u.Date >= billingPeriodStart && u.Date <= billingPeriodEnd, ct);

        var overageAmount = CalculateOverage(usageAggregates, dailyQuota, overageRatePer1k);

        // Active package subscriptions
        var activeSubs = await _packageSubscriptions.FindAsync(
            ps => ps.TenantId == tenantId && ps.Status == SubscriptionStatus.Active, ct);

        decimal packageAmount = 0m;
        if (activeSubs.Count > 0)
        {
            var packageIds = activeSubs.Select(s => s.PackageId).ToHashSet();
            var allPackages = await _pluginPackages.GetAllAsync(ct);
            packageAmount = allPackages.Where(p => packageIds.Contains(p.PackageId)).Sum(p => p.MonthlyPrice);
        }

        var totalAmount = baseAmount + overageAmount + packageAmount;
        var invoice = Invoice.Create(tenantId, billingPeriodStart, billingPeriodEnd, baseAmount, overageAmount, packageAmount);

        if (totalAmount > 0 && !string.IsNullOrEmpty(stripeCustomerId))
        {
            try
            {
                var description = $"Invoice for {billingPeriodStart:yyyy-MM}";
                var stripeInvoiceId = await _stripeService.CreateInvoiceAsync(stripeCustomerId, totalAmount, description, ct);
                invoice.SetStripeInvoiceId(stripeInvoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Stripe invoice for tenant {TenantId}", tenantId);
            }
        }

        await _invoices.AddAsync(invoice, ct);
        await _invoices.SaveChangesAsync(ct);
    }

    private static decimal CalculateOverage(IReadOnlyList<UsageAggregate> usageAggregates, int? dailyQuota, decimal? overageRatePer1k)
    {
        if (dailyQuota is null || overageRatePer1k is null || overageRatePer1k.Value <= 0) return 0m;

        long totalOverage = 0;
        foreach (var agg in usageAggregates)
        {
            var excess = agg.TotalRequests - dailyQuota.Value;
            if (excess > 0) totalOverage += excess;
        }
        return totalOverage * overageRatePer1k.Value / 1000m;
    }
}
