namespace PluginRuntime.Api.Modules.Gateway.Domain;

/// <summary>
/// Persisted failed Redis notification for retry.
/// When Redis pub/sub fails after max retries, the notification is stored here.
/// </summary>
public sealed class FailedNotification
{
    public Guid NotificationId { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private FailedNotification() { }

    public static FailedNotification Create(string channel, string payload, int retryCount)
    {
        return new FailedNotification
        {
            NotificationId = Guid.NewGuid(),
            Channel = channel,
            Payload = payload,
            RetryCount = retryCount,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }
}
