using PluginRuntime.Api.Modules.Billing.Services;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Billing.EventHandlers;

/// <summary>
/// Handles TenantCreated domain events for the Billing module.
/// Creates a Stripe customer for non-internal tenants.
/// Internal tenants are exempt from billing and do not get a Stripe customer.
/// </summary>
public sealed class TenantCreatedHandler : IDomainEventHandler<TenantCreated>
{
    private readonly IBillingService _billingService;
    private readonly ILogger<TenantCreatedHandler> _logger;

    public TenantCreatedHandler(IBillingService billingService, ILogger<TenantCreatedHandler> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(TenantCreated domainEvent, CancellationToken ct)
    {
        if (domainEvent.IsInternal)
        {
            _logger.LogInformation(
                "Skipping Stripe customer creation for internal tenant {TenantId}",
                domainEvent.TenantId);
            return;
        }

        _logger.LogInformation(
            "Creating Stripe customer for tenant {TenantId} ({Email})",
            domainEvent.TenantId, domainEvent.ContactEmail);

        await _billingService.CreateStripeCustomerAsync(
            domainEvent.TenantId,
            domainEvent.ContactEmail,
            domainEvent.Name,
            ct);
    }
}
