using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Tenants.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;
using PluginRuntime.Api.Shared.ValueObjects;

namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// Implements API key generation, revocation, and listing.
/// Keys are generated as 64-character cryptographically random strings.
/// Only the SHA-256 hash is stored; the plaintext is returned exactly once.
/// </summary>
public sealed class ApiKeyService : IApiKeyService
{
    private const int KeyLength = 64;
    private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

    private readonly AppDbContext _db;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ApiKeyService(AppDbContext db, IDomainEventDispatcher eventDispatcher)
    {
        _db = db;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<ApiKeyGenerationResult> GenerateAsync(Guid tenantId, string? name, DateTime? expiresAt, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .Include(t => t)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");

        var plan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == tenant.PlanId, ct)
            ?? throw new DomainException("Associated plan not found. Platform configuration error.");

        // Count active keys for this tenant
        var activeKeyCount = await _db.ApiKeys
            .CountAsync(k => k.TenantId == tenantId && k.Status == ApiKeyStatus.Active, ct);

        // Enforce plan's max_api_keys limit (null = unlimited)
        if (plan.MaxApiKeys.HasValue && activeKeyCount >= plan.MaxApiKeys.Value)
            throw new ApiKeyLimitException();

        // Generate 64-character cryptographically random key
        var plaintextKey = GenerateRandomKey();

        // Compute SHA-256 hash using KeyHash value object
        var keyHash = new KeyHash(plaintextKey);

        // Extract prefix (first 8 chars) and suffix (last 4 chars)
        var prefix = plaintextKey[..8];
        var suffix = plaintextKey[^4..];

        // Create ApiKey entity
        var apiKey = new ApiKey(
            keyId: Guid.NewGuid(),
            tenantId: tenantId,
            keyHash: keyHash.Value,
            keyPrefix: prefix,
            keySuffix: suffix,
            expiresAt: expiresAt);

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync(ct);

        // Return plaintext key exactly once
        return new ApiKeyGenerationResult
        {
            KeyId = apiKey.KeyId,
            PlaintextKey = plaintextKey,
            Prefix = prefix,
            ExpiresAt = expiresAt,
            CreatedAt = apiKey.CreatedAt
        };
    }

    public async Task RevokeAsync(Guid tenantId, Guid keyId, CancellationToken ct)
    {
        var apiKey = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyId == keyId, ct)
            ?? throw new DomainException($"API key with ID '{keyId}' not found.");

        // Verify the key belongs to the specified tenant
        if (apiKey.TenantId != tenantId)
            throw new DomainException($"API key '{keyId}' does not belong to tenant '{tenantId}'.");

        apiKey.Revoke();
        await _db.SaveChangesAsync(ct);

        // Load tenant to get current version for the event
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");

        // Dispatch KeyRevoked event
        await _eventDispatcher.DispatchAsync(new KeyRevoked(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenantId,
            KeyId: keyId,
            KeyHash: apiKey.KeyHash,
            Version: tenant.Version), ct);
    }

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        var keys = await _db.ApiKeys
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => ApiKeyDto.FromEntity(k))
            .ToListAsync(ct);

        return keys;
    }

    /// <summary>
    /// Generates a cryptographically random key of the specified length
    /// using characters from a-zA-Z0-9 and hyphens.
    /// </summary>
    private static string GenerateRandomKey()
    {
        Span<char> result = stackalloc char[KeyLength];
        Span<byte> randomBytes = stackalloc byte[KeyLength];
        RandomNumberGenerator.Fill(randomBytes);

        for (var i = 0; i < KeyLength; i++)
        {
            result[i] = AllowedChars[randomBytes[i] % AllowedChars.Length];
        }

        return new string(result);
    }
}
