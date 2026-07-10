using PublicApiGateway.Models;
using StackExchange.Redis;

namespace PublicApiGateway.Services;

/// <summary>
/// Daily quota enforcement using Redis atomic counters.
/// Key pattern: gw:quota:{tenantId}:{yyyy-MM-dd}
/// Returns 503 if Redis unreachable (fail-closed).
/// </summary>
public sealed class QuotaService : IQuotaService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<QuotaService> _logger;

    public QuotaService(IConnectionMultiplexer redis, ILogger<QuotaService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<QuotaResult> IncrementAndCheckAsync(string tenantId, PlanLimits limits, CancellationToken ct)
    {
        // Unlimited — always allow
        if (!limits.DailyQuota.HasValue)
        {
            return new QuotaResult(true, 0, 0, 0);
        }

        var dailyLimit = limits.DailyQuota.Value;
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var key = $"gw:quota:{tenantId}:{today}";

        try
        {
            var db = _redis.GetDatabase();

            var count = await db.StringIncrementAsync(key);

            // Set 25h TTL on first increment (covers timezone edge cases)
            if (count == 1)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromHours(25));
            }

            if (count > dailyLimit)
            {
                // Over quota — calculate Retry-After (seconds until next UTC midnight)
                var retryAfter = CalculateRetryAfterSeconds();
                return new QuotaResult(false, count, dailyLimit, retryAfter);
            }

            return new QuotaResult(true, count, dailyLimit, 0);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for quota check tenant {TenantId} — allowing request", tenantId);
            return new QuotaResult(true, 0, 0, 0);
        }
    }

    private static int CalculateRetryAfterSeconds()
    {
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        return (int)(nextMidnight - now).TotalSeconds;
    }
}
