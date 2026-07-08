namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Abstraction over the Stripe.net SDK for billing operations.
/// All methods throw <see cref="Shared.Exceptions.BillingProviderException"/> on Stripe failures.
/// </summary>
public interface IStripeService
{
    Task<string> CreateCustomerAsync(string email, string name, CancellationToken ct);
    Task<string> CreateSubscriptionAsync(string customerId, string priceId, CancellationToken ct);
    Task UpdateSubscriptionAsync(string subscriptionId, string newPriceId, bool prorate, CancellationToken ct);
    Task CancelSubscriptionItemAsync(string subscriptionItemId, bool atPeriodEnd, CancellationToken ct);
    Task<string> AddSubscriptionItemAsync(string subscriptionId, string priceId, CancellationToken ct);
    Task<string> CreateInvoiceAsync(string customerId, decimal amount, string description, CancellationToken ct);
    Task<string> CreateCreditNoteAsync(string stripeInvoiceId, decimal amount, string reason, CancellationToken ct);
    bool VerifyWebhookSignature(string payload, string signatureHeader);
}
