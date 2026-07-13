namespace PluginRuntime.Api.Shared.Entities;

/// <summary>
/// Subscription entity for extension-to-extension access requests.
/// </summary>
public sealed class Subscription
{
    public Guid SubscriptionId { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public Guid TargetExtensionId { get; set; }
    public string TargetExtensionName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Reason { get; set; }
    public object? ExpectedUsage { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
