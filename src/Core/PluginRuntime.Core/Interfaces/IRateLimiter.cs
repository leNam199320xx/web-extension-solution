using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Core.Interfaces;

public interface IRateLimiter
{
    Task<RateLimitResult> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken);
}
