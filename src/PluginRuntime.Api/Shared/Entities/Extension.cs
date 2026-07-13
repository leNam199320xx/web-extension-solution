namespace PluginRuntime.Api.Shared.Entities;

/// <summary>
/// Extension entity representing a registered plugin extension.
/// </summary>
public sealed class Extension
{
    public Guid ExtensionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public Guid PublisherId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Low";
    public string ShortDescription { get; set; } = string.Empty;
    public string Visibility { get; set; } = "public";
    public string Status { get; set; } = "Active";
    public int SubscriberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
