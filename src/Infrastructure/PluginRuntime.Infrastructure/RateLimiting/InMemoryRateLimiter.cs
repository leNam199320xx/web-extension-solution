using System.Collections.Concurrent;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Infrastructure.RateLimiting;

/// <summary>
/// In-memory rate limiter using a sliding window counter pattern.
/// Suitable for single-instance deployment and testing.
/// For multi-instance deployment, swap to <see cref="RedisRateLimiter"/> via DI configuration.
/// </summary>
public sealed class InMemoryRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _windows = new();

    public Task<RateLimitResult> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxRequests, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);

        var now = DateTime.UtcNow;
        var timestamps = _windows.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());

        // Remove expired timestamps outside the current window
        while (timestamps.TryPeek(out var oldest) && (now - oldest) > window)
        {
            timestamps.TryDequeue(out _);
        }

        var currentCount = timestamps.Count;

        if (currentCount >= maxRequests)
        {
            // Calculate retry-after based on the oldest timestamp in the window
            var retryAfter = timestamps.TryPeek(out var earliestInWindow)
                ? window - (now - earliestInWindow)
                : window;

            if (retryAfter < TimeSpan.Zero)
                retryAfter = TimeSpan.Zero;

            var result = new RateLimitResult(
                IsAllowed: false,
                Remaining: 0,
                RetryAfter: retryAfter);

            return Task.FromResult(result);
        }

        // Allow the request and record the timestamp
        timestamps.Enqueue(now);

        var remaining = maxRequests - currentCount - 1;
        var allowedResult = new RateLimitResult(
            IsAllowed: true,
            Remaining: remaining,
            RetryAfter: TimeSpan.Zero);

        return Task.FromResult(allowedResult);
    }
}
