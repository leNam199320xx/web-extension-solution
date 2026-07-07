using PluginRuntime.Core.Enums;

namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class ExtensionSubscriptionEntity
{
    public Guid SubscriptionId { get; set; }
    public string SourceExtensionId { get; set; } = "";
    public string TargetExtensionId { get; set; } = "";
    public SubscriptionStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? ExpectedUsage { get; set; }
    public string? Conditions { get; set; }
    public Guid? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
