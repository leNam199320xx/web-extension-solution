namespace PluginRuntime.Api.Modules.Subscriptions.DTOs;

/// <summary>
/// Request body for creating a new plugin package.
/// </summary>
public sealed record CreatePackageRequest
{
    /// <summary>Package display name (1–200 characters).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional description of the package.</summary>
    public string? Description { get; init; }

    /// <summary>Monthly price in base currency (must be >= 0).</summary>
    public decimal MonthlyPrice { get; init; }

    /// <summary>Plugin IDs to include in this package.</summary>
    public IReadOnlyList<Guid> PluginIds { get; init; } = [];
}
