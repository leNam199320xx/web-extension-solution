using PluginRuntime.Api.Modules.Tenants.DTOs;
using PluginRuntime.Api.Shared.DTOs;

namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// Service interface for tenant registration, lifecycle management, and listing.
/// </summary>
public interface ITenantService
{
    /// <summary>Registers a new external tenant with the Free plan.</summary>
    Task<TenantDto> RegisterAsync(TenantRegistrationRequest request, CancellationToken ct);

    /// <summary>Registers an internal tenant with the Internal plan (Platform_Admin only).</summary>
    Task<TenantDto> RegisterInternalAsync(InternalTenantRequest request, CancellationToken ct);

    /// <summary>Suspends an active tenant.</summary>
    Task SuspendAsync(Guid tenantId, string actorId, string reason, CancellationToken ct);

    /// <summary>Reactivates a suspended tenant.</summary>
    Task ReactivateAsync(Guid tenantId, string actorId, string reason, CancellationToken ct);

    /// <summary>Soft-deletes a tenant.</summary>
    Task DeleteAsync(Guid tenantId, string actorId, string reason, CancellationToken ct);

    /// <summary>Retrieves a tenant by ID, or null if not found.</summary>
    Task<TenantDto?> GetByIdAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Lists tenants with filtering and pagination.</summary>
    Task<PagedResult<TenantDto>> ListAsync(TenantFilter filter, PaginationParams paging, CancellationToken ct);
}
