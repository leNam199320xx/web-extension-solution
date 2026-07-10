using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using PublicApiGateway.Configuration;
using PublicApiGateway.Models;
using StackExchange.Redis;

namespace PublicApiGateway.Services;

/// <summary>
/// Validates API keys using Redis cache with PostgreSQL fallback.
/// Caches validated keys with configurable TTL.
/// </summary>
public sealed class ApiKeyService : IApiKeyService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly GatewayOptions _gatewayOptions;
    private readonly string _connectionString;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(
        IConnectionMultiplexer redis,
        IOptions<GatewayOptions> gatewayOptions,
        IConfiguration configuration,
        ILogger<ApiKeyService> logger)
    {
        _redis = redis;
        _gatewayOptions = gatewayOptions.Value;
        _connectionString = configuration.GetConnectionString("PostgreSQL") ?? "";
        _logger = logger;
    }

    public async Task<ApiKeyInfo?> ValidateAsync(string apiKey, CancellationToken ct)
    {
        var keyHash = ComputeSha256(apiKey);
        var cacheKey = $"gw:apikey:{keyHash}";

        // Try Redis cache first
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);

        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<ApiKeyInfo>(cached.ToString());
        }

        // Fallback to PostgreSQL
        var info = await LookupFromDatabaseAsync(keyHash, ct);

        if (info is not null)
        {
            // Cache the result
            var json = JsonSerializer.Serialize(info);
            await db.StringSetAsync(cacheKey, json, TimeSpan.FromSeconds(_gatewayOptions.CacheTtlSeconds));
        }

        return info;
    }

    public async Task InvalidateCacheAsync(string apiKey, CancellationToken ct)
    {
        var keyHash = ComputeSha256(apiKey);
        var cacheKey = $"gw:apikey:{keyHash}";

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(cacheKey);
    }

    private async Task<ApiKeyInfo?> LookupFromDatabaseAsync(string keyHash, CancellationToken ct)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand("""
                SELECT k.key_id, k.tenant_id, t.name, k.status, k.expires_at,
                       p.type, p.rate_limit, p.daily_quota
                FROM api_keys k
                JOIN tenants t ON k.tenant_id = t.tenant_id
                JOIN plans p ON t.plan_id = p.plan_id
                WHERE k.key_hash = @keyHash
                LIMIT 1
                """, conn);

            cmd.Parameters.AddWithValue("keyHash", keyHash);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return null;

            var status = reader.GetString(3) switch
            {
                "Active" => ApiKeyStatus.Active,
                "Revoked" => ApiKeyStatus.Revoked,
                _ => ApiKeyStatus.Expired
            };

            var planType = reader.GetString(5) switch
            {
                "Pro" => PlanType.Pro,
                "Enterprise" => PlanType.Enterprise,
                _ => PlanType.Free
            };

            var rateLimit = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6);
            var dailyQuota = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7);

            return new ApiKeyInfo(
                KeyId: reader.GetGuid(0),
                TenantId: reader.GetGuid(1),
                TenantName: reader.GetString(2),
                PlanType: planType,
                Status: status,
                ExpiresAt: reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Limits: new PlanLimits(rateLimit, dailyQuota));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lookup API key from database");
            return null;
        }
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
