using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;
using StackExchange.Redis;

namespace PluginRuntime.Infrastructure.RateLimiting;

/// <summary>
/// Redis-based rate limiter using INCR + EXPIRE for sliding window counters.
/// Provides distributed rate limiting across multiple instances.
/// For single-instance deployment, use <see cref="InMemoryRateLimiter"/> instead.
/// </summary>
public sealed class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimiter(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    public async Task<RateLimitResult> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxRequests, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);

        var db = _redis.GetDatabase();
        var redisKey = $"ratelimit:{key}";

        // Use a Lua script for atomic increment + expire to avoid race conditions
        var script = @"
            local current = redis.call('INCR', KEYS[1])
            if current == 1 then
                redis.call('PEXPIRE', KEYS[1], ARGV[1])
            end
            local ttl = redis.call('PTTL', KEYS[1])
            return { current, ttl }
        ";

        var windowMs = (long)window.TotalMilliseconds;

        var result = await db.ScriptEvaluateAsync(
            script,
            keys: [new RedisKey(redisKey)],
            values: [new RedisValue(windowMs.ToString())]);

        var results = (RedisResult[])result!;
        var currentCount = (long)results[0];
        var ttlMs = (long)results[1];

        if (currentCount > maxRequests)
        {
            var retryAfter = ttlMs > 0
                ? TimeSpan.FromMilliseconds(ttlMs)
                : window;

            return new RateLimitResult(
                IsAllowed: false,
                Remaining: 0,
                RetryAfter: retryAfter);
        }

        var remaining = (int)(maxRequests - currentCount);

        return new RateLimitResult(
            IsAllowed: true,
            Remaining: remaining,
            RetryAfter: TimeSpan.Zero);
    }
}
