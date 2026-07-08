using PluginRuntime.Api.Shared.Entities;

namespace PluginRuntime.Api.Modules.Tenants.DTOs;

/// <summary>
/// Filter parameters for tenant listing queries.
/// </summary>
public sealed record TenantFilter
{
    /// <summary>Filter by tenant status.</summary>
    public TenantStatus? Status { get; init; }

    /// <summary>Filter by plan ID.</summary>
    public Guid? PlanId { get; init; }

    /// <summary>Filter by internal tenant flag.</summary>
    public bool? IsInternal { get; init; }
}
