namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// High-level billing operations that orchestrate Stripe interactions
/// and persist billing state on tenant records.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Creates a Stripe customer for the given tenant and persists the stripe_customer_id.
    /// </summary>
    Task CreateStripeCustomerAsync(Guid tenantId, string email, string name, CancellationToken ct);
}
