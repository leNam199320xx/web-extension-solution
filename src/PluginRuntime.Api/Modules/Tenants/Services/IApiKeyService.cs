using PluginRuntime.Api.Modules.Tenants.DTOs;

namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// Service interface for API key generation, revocation, and listing.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a new API key for the specified tenant.
    /// Enforces the plan's max_api_keys limit.
    /// Returns the plaintext key exactly once.
    /// </summary>
    Task<ApiKeyGenerationResult> GenerateAsync(Guid tenantId, string? name, DateTime? expiresAt, CancellationToken ct);

    /// <summary>
    /// Revokes an active API key, publishing a KeyRevoked domain event.
    /// </summary>
    Task RevokeAsync(Guid tenantId, Guid keyId, CancellationToken ct);

    /// <summary>
    /// Lists all API keys for the specified tenant with masked display.
    /// </summary>
    Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid tenantId, CancellationToken ct);
}
