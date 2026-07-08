using Microsoft.Extensions.Options;
using PluginRuntime.Api.Modules.Billing.Configuration;
using PluginRuntime.Api.Shared.Exceptions;
using Stripe;

namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Wraps the Stripe.net SDK, translating StripeException into BillingProviderException.
/// </summary>
public sealed class StripeService : IStripeService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IOptions<StripeOptions> options, ILogger<StripeService> logger)
    {
        _options = options.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<string> CreateCustomerAsync(string email, string name, CancellationToken ct)
    {
        try
        {
            var service = new CustomerService();
            var customer = await service.CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Name = name
            }, cancellationToken: ct);

            _logger.LogInformation("Created Stripe customer {CustomerId} for {Email}", customer.Id, email);
            return customer.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating customer for {Email}", email);
            throw new BillingProviderException(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<string> CreateSubscriptionAsync(string customerId, string priceId, CancellationToken ct)
    {
        try
        {
            var service = new SubscriptionService();
            var subscription = await service.CreateAsync(new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = [new SubscriptionItemOptions { Price = priceId }]
            }, cancellationToken: ct);

            _logger.LogInformation("Created Stripe subscription {SubscriptionId} for customer {CustomerId}",
                subscription.Id, customerId);
            return subscription.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating subscription for customer {CustomerId}", customerId);
            throw new BillingProviderException(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task UpdateSubscriptionAsync(string subscriptionId, string newPriceId, bool prorate, CancellationToken ct)
    {
        try
        {
            var service = new SubscriptionService();
            var subscription = await service.GetAsync(subscriptionId, cancellationToken: ct);
            var itemId = subscription.Items.Data[0].Id;

            await service.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
            {
                Items =
                [
                    new SubscriptionItemOptions
                    {
                        Id = itemId,
                        Price = newPriceId
                    }
                ],
                ProrationBehavior = prorate ? "create_prorations" : "none"
            }, cancellationToken: ct);

            _logger.LogInformation("Updated Stripe subscription {SubscriptionId} to price {PriceId}, prorate={Prorate}",
                subscriptionId, newPriceId, prorate);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error updating subscription {SubscriptionId}", subscriptionId);
            throw new BillingProviderException(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task CancelSubscriptionItemAsync(string subscriptionItemId, bool atPeriodEnd, CancellationToken ct)
    {
        try
        {
            var service = new SubscriptionItemService();

            if (atPeriodEnd)
            {
                // Clear usage and mark for cancellation at period end by removing the item with proration
                await service.DeleteAsync(subscriptionItemId, new SubscriptionItemDeleteOptions
                {
                    ProrationBehavior = "none"
                }, cancellationToken: ct);
            }
            else
            {
                await service.DeleteAsync(subscriptionItemId, new SubscriptionItemDeleteOptions
                {
                    ProrationBehavior = "create_prorations"
                }, cancellationToken: ct);
            }

            _logger.LogInformation("Cancelled Stripe subscription item {SubscriptionItemId}, atPeriodEnd={AtPeriodEnd}",
                subscriptionItemId, atPeriodEnd);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error cancelling subscription item {SubscriptionItemId}", subscriptionItemId);
            throw new BillingProviderException(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<string> AddSubscriptionItemAsync(string subscriptionId, string priceId, CancellationToken ct)
    {
        try
        {
            var service = new SubscriptionItemService();
            var item = await service.CreateAsync(new SubscriptionItemCreateOptions
            {
                Subscription = subscriptionId,
                Price = priceId
            }, cancellationToken: ct);

            _logger.LogInformation("Added subscription item {ItemId} to subscription {SubscriptionId} with price {PriceId}",
                item.Id, subscriptionId, priceId);
            return item.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error adding item to subscription {SubscriptionId}", subscriptionId);
            throw new BillingProviderException(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<string> CreateInvoiceAsync(string customerId, decimal amount, string description, CancellationToken ct)
    {
        try
        {
            // Create an invoice item, then finalize the invoice
            var invoiceItemService = new InvoiceItemService();
            await invoiceItemService.CreateAsync(new InvoiceItemCreateOptions
            {
                Customer = customerId,
                Amount = (long)(amount * 100), // Convert to cents
                Currency = "usd",
                Description = description
            }, cancellationToken: ct);

            var invoiceService = new Stripe.InvoiceService();
            var invoice = await invoiceService.CreateAsync(new InvoiceCreateOptions
            {
                Customer = customerId,
                AutoAdvance = true
            }, cancellationToken: ct);

            _logger.LogInformation("Created Stripe invoice {InvoiceId} for customer {CustomerId}, amount={Amount}",
                invoice.Id, customerId, amount);
            return invoice.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating invoice for customer {CustomerId}", customerId);
            throw new BillingProviderException(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<string> CreateCreditNoteAsync(string stripeInvoiceId, decimal amount, string reason, CancellationToken ct)
    {
        try
        {
            var service = new CreditNoteService();
            var creditNote = await service.CreateAsync(new CreditNoteCreateOptions
            {
                Invoice = stripeInvoiceId,
                Lines =
                [
                    new CreditNoteLineOptions
                    {
                        Type = "custom_line_item",
                        UnitAmount = (long)(amount * 100),
                        Quantity = 1,
                        Description = reason
                    }
                ],
                Reason = "other",
                Memo = reason
            }, cancellationToken: ct);

            _logger.LogInformation("Created Stripe credit note {CreditNoteId} for invoice {InvoiceId}, amount={Amount}",
                creditNote.Id, stripeInvoiceId, amount);
            return creditNote.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating credit note for invoice {InvoiceId}", stripeInvoiceId);
            throw new BillingProviderException(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public bool VerifyWebhookSignature(string payload, string signatureHeader)
    {
        try
        {
            EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);
            return true;
        }
        catch (StripeException)
        {
            _logger.LogWarning("Webhook signature verification failed");
            return false;
        }
    }
}
