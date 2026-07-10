using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Orchestrates billing operations using IRepository for provider-agnostic persistence.
/// </summary>
public sealed class BillingService : IBillingService
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IStripeService _stripeService;
    private readonly ILogger<BillingService> _logger;

    public BillingService(IRepository<Tenant> tenants, IStripeService stripeService, ILogger<BillingService> logger)
    {
        _tenants = tenants;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task CreateStripeCustomerAsync(Guid tenantId, string email, string name, CancellationToken ct)
    {
        var stripeCustomerId = await _stripeService.CreateCustomerAsync(email, name, ct);

        var tenant = await _tenants.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant '{tenantId}' not found.");

        tenant.SetStripeCustomerId(stripeCustomerId);
        await _tenants.UpdateAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);

        _logger.LogInformation("Associated Stripe customer {StripeCustomerId} with tenant {TenantId}", stripeCustomerId, tenantId);
    }
}
