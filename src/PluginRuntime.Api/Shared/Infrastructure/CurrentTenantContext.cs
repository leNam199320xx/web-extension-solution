using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Infrastructure;

/// <summary>
/// Scoped implementation of ICurrentTenantContext.
/// Populated by tenant-resolution middleware after JWT authentication.
/// Properties are mutable so middleware can set values per-request.
/// </summary>
public sealed class CurrentTenantContext : ICurrentTenantContext
{
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? PlanId { get; set; }
    public bool IsInternal { get; set; }
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Sets the tenant context from middleware after JWT validation.
    /// </summary>
    public void SetContext(Guid? tenantId, Guid? planId, bool isInternal, bool isAdmin)
    {
        TenantId = tenantId;
        PlanId = planId;
        IsInternal = isInternal;
        IsAdmin = isAdmin;
    }
}
