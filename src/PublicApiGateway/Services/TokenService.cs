using StackExchange.Redis;

namespace PublicApiGateway.Services;

/// <summary>
/// Service token acquisition. Gracefully handles Redis unavailability.
/// </summary>
public sealed class TokenService : ITokenService
{
    private const string CacheKey = "gw:servicetoken";
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<TokenService> _logger;

    // Fallback in-memory token when Redis is unavailable
    private string? _fallbackToken;
    private DateTime _fallbackExpiry = DateTime.MinValue;

    public TokenService(IConnectionMultiplexer redis, ILogger<TokenService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string> GetServiceTokenAsync(CancellationToken ct)
    {
        try
        {
            if (_redis.IsConnected)
            {
                var db = _redis.GetDatabase();
                var cached = await db.StringGetAsync(CacheKey);
                if (cached.HasValue) return cached.ToString();

                var token = GenerateToken();
                await db.StringSetAsync(CacheKey, token, TimeSpan.FromMinutes(4));
                return token;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Redis unavailable for token cache — using in-memory fallback");
        }

        // Fallback: in-memory token
        if (_fallbackToken is null || DateTime.UtcNow > _fallbackExpiry)
        {
            _fallbackToken = GenerateToken();
            _fallbackExpiry = DateTime.UtcNow.AddMinutes(4);
        }

        return _fallbackToken;
    }

    private static string GenerateToken() => $"gw-service-token-{Guid.NewGuid():N}";
}
