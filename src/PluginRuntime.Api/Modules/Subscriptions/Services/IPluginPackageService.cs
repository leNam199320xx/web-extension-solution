using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Shared.DTOs;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Admin service for managing plugin packages (CRUD operations).
/// </summary>
public interface IPluginPackageService
{
    /// <summary>Creates a new plugin package.</summary>
    Task<PluginPackageDto> CreateAsync(CreatePackageRequest request, CancellationToken ct);

    /// <summary>Updates an existing plugin package's details and composition.</summary>
    Task<PluginPackageDto> UpdateAsync(Guid packageId, UpdatePackageRequest request, CancellationToken ct);

    /// <summary>Deactivates a package (preserves existing subscriptions, prevents new ones).</summary>
    Task DeactivateAsync(Guid packageId, CancellationToken ct);

    /// <summary>Lists active packages with pagination (default 20, max 100).</summary>
    Task<PagedResult<PluginPackageDto>> ListActiveAsync(PaginationParams paging, CancellationToken ct);
}
