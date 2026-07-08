namespace PluginRuntime.Api.Shared.Interfaces;

/// <summary>
/// Resolves the current tenant from the HTTP context.
/// Populated by middleware after JWT authentication and tenant resolution.
/// </summary>
public interface ICurrentTenantContext
{
    /// <summary>The authenticated tenant's unique identifier, or null if not resolved.</summary>
    Guid? TenantId { get; }

    /// <summary>The tenant's display name, or null if not resolved.</summary>
    string? TenantName { get; }

    /// <summary>The tenant's current plan identifier, or null if not resolved.</summary>
    Guid? PlanId { get; }

    /// <summary>Whether the tenant is an internal platform service (Internal plan).</summary>
    bool IsInternal { get; }

    /// <summary>Whether the authenticated user has Platform_Admin privileges.</summary>
    bool IsAdmin { get; }
}
