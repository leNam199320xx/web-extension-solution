namespace PluginRuntime.Api.Modules.Tenants.DTOs;

/// <summary>
/// Request body for internal tenant registration (Platform_Admin only).
/// </summary>
public sealed record InternalTenantRequest
{
    /// <summary>Tenant name (1–200 characters).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Contact email (RFC 5322 format).</summary>
    public string ContactEmail { get; init; } = string.Empty;

    /// <summary>Optional company name.</summary>
    public string? CompanyName { get; init; }
}
