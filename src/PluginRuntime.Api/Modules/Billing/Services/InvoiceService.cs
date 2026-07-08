using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Billing.Domain;
using PluginRuntime.Api.Modules.Billing.DTOs;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Infrastructure;

namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Generates consolidated monthly invoices with base plan, overage, and package fees.
/// Calculates overage from daily usage exceeding the plan's daily quota.
/// </summary>
public sealed class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly IStripeService _stripeService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(AppDbContext db, IStripeService stripeService, ILogger<InvoiceService> logger)
    {
        _db = db;
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task GenerateMonthlyInvoicesAsync(CancellationToken ct)
    {
        // 1. Determine billing period (previous month)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var billingPeriodStart = today.AddMonths(-1).AddDays(-(today.Day - 1));
        billingPeriodStart = new DateOnly(billingPeriodStart.Year, billingPeriodStart.Month, 1);
        var billingPeriodEnd = billingPeriodStart.AddMonths(1).AddDays(-1);

        _logger.LogInformation(
            "Generating monthly invoices for billing period {Start} to {End}",
            billingPeriodStart, billingPeriodEnd);

        // 2. Load all billable tenants (those on billable plans)
        var billableTenants = await _db.Tenants
            .Join(_db.Plans, t => t.PlanId, p => p.PlanId, (t, p) => new { Tenant = t, Plan = p })
            .Where(tp => tp.Plan.IsBillable)
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} billable tenants", billableTenants.Count);

        foreach (var tp in billableTenants)
        {
            try
            {
                await GenerateInvoiceForTenantAsync(
                    tp.Tenant.TenantId,
                    tp.Tenant.StripeCustomerId,
                    tp.Plan.MonthlyPrice,
                    tp.Plan.DailyQuota,
                    tp.Plan.OverageRatePer1k,
                    billingPeriodStart,
                    billingPeriodEnd,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to generate invoice for tenant {TenantId}",
                    tp.Tenant.TenantId);
                // Continue processing other tenants
            }
        }

        _logger.LogInformation("Monthly invoice generation completed");
    }

    /// <inheritdoc />
    public async Task UpdateInvoiceStatusAsync(string stripeInvoiceId, InvoiceStatus newStatus, CancellationToken ct)
    {
        var invoice = await _db.Set<Invoice>()
            .FirstOrDefaultAsync(i => i.StripeInvoiceId == stripeInvoiceId, ct);

        if (invoice is null)
        {
            _logger.LogWarning(
                "Invoice not found for Stripe invoice ID {StripeInvoiceId}",
                stripeInvoiceId);
            return;
        }

        invoice.UpdateStatus(newStatus);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated invoice {InvoiceId} status to {Status} for Stripe invoice {StripeInvoiceId}",
            invoice.InvoiceId, newStatus, stripeInvoiceId);
    }

    /// <inheritdoc />
    public async Task<PagedResult<InvoiceDto>> GetTenantInvoicesAsync(
        Guid tenantId, PaginationParams paging, CancellationToken ct)
    {
        var normalized = paging.Normalize();

        var query = _db.Set<Invoice>()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.BillingPeriodStart);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .ToListAsync(ct);

        return new PagedResult<InvoiceDto>
        {
            Items = items.Select(InvoiceDto.FromEntity).ToList(),
            Page = normalized.Page,
            PageSize = normalized.PageSize,
            TotalCount = totalCount
        };
    }

    private async Task GenerateInvoiceForTenantAsync(
        Guid tenantId,
        string? stripeCustomerId,
        decimal baseAmount,
        int? dailyQuota,
        decimal? overageRatePer1k,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        CancellationToken ct)
    {
        // a. Base amount = plan.MonthlyPrice (already passed in)

        // b. Load UsageAggregates for the billing period
        var usageAggregates = await _db.Set<UsageAggregate>()
            .Where(u => u.TenantId == tenantId
                     && u.Date >= billingPeriodStart
                     && u.Date <= billingPeriodEnd)
            .ToListAsync(ct);

        // c. Calculate overage: sum of MAX(0, daily_total - plan.DailyQuota) for each day × plan.OverageRatePer1k / 1000
        decimal overageAmount = CalculateOverage(usageAggregates, dailyQuota, overageRatePer1k);

        // d. Load active package subscriptions for the tenant, calculate package amount
        var activePackageSubscriptions = await _db.Set<PackageSubscription>()
            .Where(ps => ps.TenantId == tenantId && ps.Status == SubscriptionStatus.Active)
            .Select(ps => ps.PackageId)
            .ToListAsync(ct);

        decimal packageAmount = 0m;
        if (activePackageSubscriptions.Count > 0)
        {
            packageAmount = await _db.PluginPackages
                .Where(p => activePackageSubscriptions.Contains(p.PackageId))
                .SumAsync(p => p.MonthlyPrice, ct);
        }

        // e. Total = base + overage + packages
        var totalAmount = baseAmount + overageAmount + packageAmount;

        // f. Create Invoice entity
        var invoice = Invoice.Create(
            tenantId,
            billingPeriodStart,
            billingPeriodEnd,
            baseAmount,
            overageAmount,
            packageAmount);

        // g. If totalAmount > 0 and tenant has StripeCustomerId → call IStripeService.CreateInvoiceAsync
        if (totalAmount > 0 && !string.IsNullOrEmpty(stripeCustomerId))
        {
            try
            {
                var description = $"Invoice for {billingPeriodStart:yyyy-MM} " +
                    $"(Base: {baseAmount:C}, Overage: {overageAmount:C}, Packages: {packageAmount:C})";

                var stripeInvoiceId = await _stripeService.CreateInvoiceAsync(
                    stripeCustomerId, totalAmount, description, ct);

                invoice.SetStripeInvoiceId(stripeInvoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create Stripe invoice for tenant {TenantId}. Invoice saved locally as pending.",
                    tenantId);
                // Invoice will remain in Pending status without a Stripe ID
            }
        }

        // h. Save
        _db.Set<Invoice>().Add(invoice);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Generated invoice {InvoiceId} for tenant {TenantId}: " +
            "Base={BaseAmount}, Overage={OverageAmount}, Packages={PackageAmount}, Total={TotalAmount}",
            invoice.InvoiceId, tenantId, baseAmount, overageAmount, packageAmount, totalAmount);
    }

    private static decimal CalculateOverage(
        IReadOnlyList<UsageAggregate> usageAggregates,
        int? dailyQuota,
        decimal? overageRatePer1k)
    {
        // No overage if plan has unlimited quota or no overage rate
        if (dailyQuota is null || overageRatePer1k is null || overageRatePer1k.Value <= 0)
            return 0m;

        long totalOverageRequests = 0;
        foreach (var aggregate in usageAggregates)
        {
            var excess = aggregate.TotalRequests - dailyQuota.Value;
            if (excess > 0)
            {
                totalOverageRequests += excess;
            }
        }

        // overage = total excess requests × overageRatePer1k / 1000
        return totalOverageRequests * overageRatePer1k.Value / 1000m;
    }
}
