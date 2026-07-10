using System.Security.Cryptography;
using PluginRuntime.Api.Modules.Tenants.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Interfaces;
using PluginRuntime.Api.Shared.ValueObjects;

namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// API key generation, revocation, and listing using IRepository.
/// </summary>
public sealed class ApiKeyService : IApiKeyService
{
    private const int KeyLength = 64;
    private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<Plan> _plans;
    private readonly IRepository<ApiKey> _apiKeys;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ApiKeyService(
        IRepository<Tenant> tenants,
        IRepository<Plan> plans,
        IRepository<ApiKey> apiKeys,
        IDomainEventDispatcher eventDispatcher)
    {
        _tenants = tenants;
        _plans = plans;
        _apiKeys = apiKeys;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<ApiKeyGenerationResult> GenerateAsync(Guid tenantId, string? name, DateTime? expiresAt, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct)
            ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");

        var plan = await _plans.GetByIdAsync(tenant.PlanId, ct)
            ?? throw new DomainException("Associated plan not found. Platform configuration error.");

        var activeKeyCount = await _apiKeys.CountAsync(k => k.TenantId == tenantId && k.Status == ApiKeyStatus.Active, ct);

        if (plan.MaxApiKeys.HasValue && activeKeyCount >= plan.MaxApiKeys.Value)
            throw new ApiKeyLimitException();

        var plaintextKey = GenerateRandomKey();
        var keyHash = new KeyHash(plaintextKey);
        var prefix = plaintextKey[..8];
        var suffix = plaintextKey[^4..];

        var apiKey = new ApiKey(
            keyId: Guid.NewGuid(),
            tenantId: tenantId,
            keyHash: keyHash.Value,
            keyPrefix: prefix,
            keySuffix: suffix,
            expiresAt: expiresAt);

        await _apiKeys.AddAsync(apiKey, ct);
        await _apiKeys.SaveChangesAsync(ct);

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
        var keys = await _apiKeys.FindAsync(k => k.KeyId == keyId, ct);
        var apiKey = keys.FirstOrDefault()
            ?? throw new DomainException($"API key with ID '{keyId}' not found.");

        if (apiKey.TenantId != tenantId)
            throw new DomainException($"API key '{keyId}' does not belong to tenant '{tenantId}'.");

        apiKey.Revoke();
        await _apiKeys.UpdateAsync(apiKey, ct);
        await _apiKeys.SaveChangesAsync(ct);

        var tenant = await _tenants.GetByIdAsync(tenantId, ct)
            ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");

        await _eventDispatcher.DispatchAsync(new KeyRevoked(
            EventId: Guid.NewGuid(), OccurredAt: DateTime.UtcNow,
            TenantId: tenantId, KeyId: keyId, KeyHash: apiKey.KeyHash, Version: tenant.Version), ct);
    }

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        var keys = await _apiKeys.FindAsync(k => k.TenantId == tenantId, ct);
        return keys.OrderByDescending(k => k.CreatedAt).Select(ApiKeyDto.FromEntity).ToList();
    }

    private static string GenerateRandomKey()
    {
        Span<char> result = stackalloc char[KeyLength];
        Span<byte> randomBytes = stackalloc byte[KeyLength];
        RandomNumberGenerator.Fill(randomBytes);
        for (var i = 0; i < KeyLength; i++)
            result[i] = AllowedChars[randomBytes[i] % AllowedChars.Length];
        return new string(result);
    }
}
