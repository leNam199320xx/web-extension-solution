namespace PluginRuntime.Api.Modules.Subscriptions.Domain;

/// <summary>
/// Represents a bundle of plugins offered as a subscription package.
/// </summary>
public sealed class PluginPackage
{
    public Guid PackageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public PackageStatus Status { get; private set; }
    public string? StripePriceId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public ICollection<PackagePlugin> Plugins { get; private set; } = new List<PackagePlugin>();

    private PluginPackage() { }

    public static PluginPackage Create(string name, string? description, decimal monthlyPrice, IEnumerable<Guid> pluginIds)
    {
        var package = new PluginPackage
        {
            PackageId = Guid.NewGuid(),
            Name = name,
            Description = description,
            MonthlyPrice = monthlyPrice,
            Status = PackageStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var pluginId in pluginIds)
        {
            package.Plugins.Add(PackagePlugin.Create(package.PackageId, pluginId));
        }

        return package;
    }

    public void Update(string name, string? description, decimal monthlyPrice)
    {
        Name = name;
        Description = description;
        MonthlyPrice = monthlyPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public (IReadOnlyList<Guid> Added, IReadOnlyList<Guid> Removed) UpdatePlugins(IEnumerable<Guid> newPluginIds)
    {
        var currentIds = Plugins.Select(p => p.PluginId).ToHashSet();
        var newIds = newPluginIds.ToHashSet();

        var added = newIds.Except(currentIds).ToList();
        var removed = currentIds.Except(newIds).ToList();

        // Remove plugins no longer in the set
        var toRemove = Plugins.Where(p => removed.Contains(p.PluginId)).ToList();
        foreach (var plugin in toRemove)
        {
            Plugins.Remove(plugin);
        }

        // Add new plugins
        foreach (var pluginId in added)
        {
            Plugins.Add(PackagePlugin.Create(PackageId, pluginId));
        }

        UpdatedAt = DateTime.UtcNow;
        return (added, removed);
    }

    public void Deactivate()
    {
        Status = PackageStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum PackageStatus
{
    Active,
    Inactive
}
