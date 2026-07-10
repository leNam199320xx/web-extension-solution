namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Processes incoming Stripe webhook events with idempotent handling.
/// </summary>
public interface IWebhookService
{
    Task<bool> ProcessAsync(string payload, string signatureHeader, CancellationToken ct);
}
