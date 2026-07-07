using PluginRuntime.Core.Enums;

namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class ExtensionRegistryEntity
{
    public string ExtensionId { get; set; } = "";
    public Guid PluginId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public Guid AuthorId { get; set; }
    public Visibility Visibility { get; set; }
    public string? Category { get; set; }
    public string? LatestVersion { get; set; }
    public int TotalVersions { get; set; }
    public int SubscriberCount { get; set; }
    public string? InvocationPolicy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
