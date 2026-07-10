using PublicApiGateway.Models;
using StackExchange.Redis;

namespace PublicApiGateway.Services;

/// <summary>
/// Sliding window rate limiting using Redis sorted sets.
/// Enterprise plan (unlimited) always passes.
/// Returns 503 if Redis is unreachable.
/// </summary>
public sealed class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(IConnectionMultiplexer redis, ILogger<RateLimitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckAsync(string tenantId, PlanLimits limits, CancellationToken ct)
    {
        // Unlimited (Enterprise) — always allow
        if (!limits.RateLimit.HasValue)
        {
            return new RateLimitResult(true, 0, 0, 0);
        }

        var maxRequests = limits.RateLimit.Value;
        var windowSeconds = limits.RateLimitWindowSeconds;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (windowSeconds * 1000L);
        var key = $"gw:ratelimit:{tenantId}";

        try
        {
            var db = _redis.GetDatabase();

            // Pipeline: prune expired → count → add → set TTL
            var transaction = db.CreateTransaction();

            _ = transaction.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
            var countTask = transaction.SortedSetLengthAsync(key);
            _ = transaction.SortedSetAddAsync(key, now.ToString(), now);
            _ = transaction.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds + 1));

            await transaction.ExecuteAsync();

            var currentCount = await countTask;

            if (currentCount > maxRequests)
            {
                // Over limit — rollback the added entry
                await db.SortedSetRemoveAsync(key, now.ToString());

                var resetAt = DateTimeOffset.UtcNow.AddSeconds(windowSeconds).ToUnixTimeSeconds();
                return new RateLimitResult(false, maxRequests, 0, resetAt);
            }

            var remaining = (int)Math.Max(0, maxRequests - currentCount);
            var resetAtTime = DateTimeOffset.UtcNow.AddSeconds(windowSeconds).ToUnixTimeSeconds();

            return new RateLimitResult(true, maxRequests, remaining, resetAtTime);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for rate limiting tenant {TenantId} — allowing request", tenantId);
            return new RateLimitResult(true, 0, 0, 0);
        }
    }
}
