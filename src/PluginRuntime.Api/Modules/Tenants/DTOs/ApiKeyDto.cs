using PluginRuntime.Api.Shared.Entities;

namespace PluginRuntime.Api.Modules.Tenants.DTOs;

/// <summary>
/// Data transfer object representing an API key in API responses.
/// The middle portion of the key is masked for security.
/// </summary>
public sealed record ApiKeyDto
{
    public Guid KeyId { get; init; }
    public Guid TenantId { get; init; }
    public string MaskedKey { get; init; } = string.Empty;
    public ApiKeyStatus Status { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RevokedAt { get; init; }

    public static ApiKeyDto FromEntity(ApiKey key) => new()
    {
        KeyId = key.KeyId,
        TenantId = key.TenantId,
        MaskedKey = $"{key.KeyPrefix}...{key.KeySuffix}",
        Status = key.Status,
        ExpiresAt = key.ExpiresAt,
        CreatedAt = key.CreatedAt,
        RevokedAt = key.RevokedAt
    };
}
