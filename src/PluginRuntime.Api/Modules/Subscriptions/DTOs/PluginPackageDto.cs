using PluginRuntime.Api.Modules.Subscriptions.Domain;

namespace PluginRuntime.Api.Modules.Subscriptions.DTOs;

/// <summary>
/// Read model for a plugin package.
/// </summary>
public sealed record PluginPackageDto
{
    public Guid PackageId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public PackageStatus Status { get; init; }
    public string? StripePriceId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<Guid> PluginIds { get; init; } = [];

    public static PluginPackageDto FromEntity(PluginPackage entity) => new()
    {
        PackageId = entity.PackageId,
        Name = entity.Name,
        Description = entity.Description,
        MonthlyPrice = entity.MonthlyPrice,
        Status = entity.Status,
        StripePriceId = entity.StripePriceId,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        PluginIds = entity.Plugins.Select(p => p.PluginId).ToList()
    };
}
