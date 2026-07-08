using PluginRuntime.Api.Shared.Entities;

namespace PluginRuntime.Api.Modules.Tenants.DTOs;

/// <summary>
/// Data transfer object representing a Tenant in API responses.
/// </summary>
public sealed record TenantDto
{
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public TenantStatus Status { get; init; }
    public Guid PlanId { get; init; }
    public bool IsInternal { get; init; }
    public long Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static TenantDto FromEntity(Tenant tenant) => new()
    {
        TenantId = tenant.TenantId,
        Name = tenant.Name,
        ContactEmail = tenant.ContactEmail.Value,
        CompanyName = tenant.CompanyName,
        Status = tenant.Status,
        PlanId = tenant.PlanId,
        IsInternal = tenant.IsInternal,
        Version = tenant.Version,
        CreatedAt = tenant.CreatedAt,
        UpdatedAt = tenant.UpdatedAt
    };
}
