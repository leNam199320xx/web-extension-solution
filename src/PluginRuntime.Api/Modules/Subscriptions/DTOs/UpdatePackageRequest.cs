namespace PluginRuntime.Api.Modules.Subscriptions.DTOs;

/// <summary>
/// Request body for updating an existing plugin package.
/// </summary>
public sealed record UpdatePackageRequest
{
    /// <summary>Updated package display name (1–200 characters).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Updated optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Updated monthly price (must be >= 0).</summary>
    public decimal MonthlyPrice { get; init; }

    /// <summary>Updated list of plugin IDs in this package.</summary>
    public IReadOnlyList<Guid> PluginIds { get; init; } = [];
}
