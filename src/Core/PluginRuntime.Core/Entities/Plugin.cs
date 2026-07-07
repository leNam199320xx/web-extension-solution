using PluginRuntime.Core.Enums;

namespace PluginRuntime.Core.Entities;

public class Plugin
{
    public Guid PluginId { get; init; }
    public string Name { get; init; }
    public string DisplayName { get; init; }
    public string? Description { get; init; }
    public Guid OwnerId { get; init; }
    public PluginStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    public Plugin(
        Guid pluginId,
        string name,
        string displayName,
        Guid ownerId,
        PluginStatus status = PluginStatus.Active,
        string? description = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null,
        DateTime? deletedAt = null)
    {
        if (pluginId == Guid.Empty)
            throw new ArgumentException("PluginId must not be empty.", nameof(pluginId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must not be null or empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName must not be null or empty.", nameof(displayName));
        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId must not be empty.", nameof(ownerId));

        PluginId = pluginId;
        Name = name;
        DisplayName = displayName;
        Description = description;
        OwnerId = ownerId;
        Status = status;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        UpdatedAt = updatedAt ?? DateTime.UtcNow;
        DeletedAt = deletedAt;
    }
}
