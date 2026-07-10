namespace PluginRuntime.Api.Modules.Billing.Domain;

/// <summary>
/// Records a received Stripe webhook event for idempotent processing.
/// </summary>
public sealed class WebhookEvent
{
    public Guid EventId { get; private set; }
    public string StripeEventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public WebhookStatus Status { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private WebhookEvent() { }

    public static WebhookEvent Create(string stripeEventId, string eventType, string payload)
    {
        return new WebhookEvent
        {
            EventId = Guid.NewGuid(),
            StripeEventId = stripeEventId,
            EventType = eventType,
            Payload = payload,
            Status = WebhookStatus.Processing,
            ReceivedAt = DateTime.UtcNow,
            ProcessedAt = null
        };
    }

    public void MarkProcessed()
    {
        Status = WebhookStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = WebhookStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
    }
}

public enum WebhookStatus
{
    Processing,
    Processed,
    Failed
}
