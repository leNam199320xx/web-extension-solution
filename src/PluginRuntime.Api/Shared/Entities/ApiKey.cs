using PluginRuntime.Api.Shared.Exceptions;

namespace PluginRuntime.Api.Shared.Entities;

/// <summary>
/// ApiKey entity representing a tenant's API key.
/// Stores only the hash, prefix, and suffix — never the full plaintext key.
/// </summary>
public sealed class ApiKey
{
    public Guid KeyId { get; private set; }
    public Guid TenantId { get; private set; }
    public string KeyHash { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public string KeySuffix { get; private set; } = string.Empty;
    public ApiKeyStatus Status { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private ApiKey() { }

    public ApiKey(
        Guid keyId,
        Guid tenantId,
        string keyHash,
        string keyPrefix,
        string keySuffix,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new DomainException("Key hash cannot be empty.");

        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new DomainException("Key prefix cannot be empty.");

        if (string.IsNullOrWhiteSpace(keySuffix))
            throw new DomainException("Key suffix cannot be empty.");

        KeyId = keyId;
        TenantId = tenantId;
        KeyHash = keyHash;
        KeyPrefix = keyPrefix;
        KeySuffix = keySuffix;
        Status = ApiKeyStatus.Active;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes the API key, making it permanently unusable.
    /// </summary>
    public void Revoke()
    {
        if (Status != ApiKeyStatus.Active)
            throw new DomainException($"Cannot revoke key with status '{Status}'. Only active keys can be revoked.");

        Status = ApiKeyStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the API key as expired.
    /// </summary>
    public void Expire()
    {
        if (Status != ApiKeyStatus.Active)
            throw new DomainException($"Cannot expire key with status '{Status}'. Only active keys can be expired.");

        Status = ApiKeyStatus.Expired;
    }
}
