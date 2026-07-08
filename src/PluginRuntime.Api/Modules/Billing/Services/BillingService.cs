using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Shared.Infrastructure;

namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Orchestrates billing operations: creates Stripe customers via IStripeService
/// and persists the association on the tenant record.
/// </summary>
public sealed class BillingService : IBillingService
{
    private readonly AppDbContext _db;
    private readonly IStripeService _stripeService;
    private readonly ILogger<BillingService> _logger;

    public BillingService(AppDbContext db, IStripeService stripeService, ILogger<BillingService> logger)
    {
        _db = db;
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task CreateStripeCustomerAsync(Guid tenantId, string email, string name, CancellationToken ct)
    {
        var stripeCustomerId = await _stripeService.CreateCustomerAsync(email, name, ct);

        var tenant = await _db.Tenants.FirstAsync(t => t.TenantId == tenantId, ct);
        tenant.SetStripeCustomerId(stripeCustomerId);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Associated Stripe customer {StripeCustomerId} with tenant {TenantId}",
            stripeCustomerId, tenantId);
    }
}
