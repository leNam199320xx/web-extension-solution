using System.Text.Json;
using PluginRuntime.Api.Modules.Billing.Domain;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Processes Stripe webhooks with idempotent handling using IRepository.
/// </summary>
public sealed class WebhookService : IWebhookService
{
    private readonly IStripeService _stripeService;
    private readonly IInvoiceService _invoiceService;
    private readonly IRepository<WebhookEvent> _webhookEvents;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IStripeService stripeService,
        IInvoiceService invoiceService,
        IRepository<WebhookEvent> webhookEvents,
        ILogger<WebhookService> logger)
    {
        _stripeService = stripeService;
        _invoiceService = invoiceService;
        _webhookEvents = webhookEvents;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(string payload, string signatureHeader, CancellationToken ct)
    {
        if (!_stripeService.VerifyWebhookSignature(payload, signatureHeader))
        {
            _logger.LogWarning("Webhook signature verification failed");
            return false;
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var stripeEventId = root.GetProperty("id").GetString()!;
        var eventType = root.GetProperty("type").GetString()!;

        // Idempotent check
        var existing = await _webhookEvents.FindAsync(e => e.StripeEventId == stripeEventId, ct);
        if (existing.Count > 0)
        {
            _logger.LogInformation("Duplicate webhook event {StripeEventId} — skipping", stripeEventId);
            return true;
        }

        var webhookEvent = WebhookEvent.Create(stripeEventId, eventType, payload);
        await _webhookEvents.AddAsync(webhookEvent, ct);
        await _webhookEvents.SaveChangesAsync(ct);

        try
        {
            switch (eventType)
            {
                case "invoice.payment_succeeded":
                    await _invoiceService.UpdateInvoiceStatusAsync(ExtractInvoiceId(root), InvoiceStatus.Paid, ct);
                    break;
                case "invoice.payment_failed":
                    await _invoiceService.UpdateInvoiceStatusAsync(ExtractInvoiceId(root), InvoiceStatus.Failed, ct);
                    break;
                default:
                    _logger.LogInformation("Unhandled webhook event type {EventType}", eventType);
                    break;
            }

            webhookEvent.MarkProcessed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook {StripeEventId}", stripeEventId);
            webhookEvent.MarkFailed();
        }

        await _webhookEvents.UpdateAsync(webhookEvent, ct);
        await _webhookEvents.SaveChangesAsync(ct);
        return true;
    }

    private static string ExtractInvoiceId(JsonElement root) =>
        root.GetProperty("data").GetProperty("object").GetProperty("id").GetString()!;
}
