using PluginRuntime.Core.Exceptions;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Extension;

/// <summary>
/// Enforces per-caller rate limit from target's invocation_policy.rate_limit_per_caller.
/// Uses the distributed IRateLimiter abstraction for single/multi-instance support.
/// </summary>
public class InvocationRateLimiter
{
    private readonly IRateLimiter _rateLimiter;

    public InvocationRateLimiter(IRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
    }

    /// <summary>
    /// Validates that the caller has not exceeded the per-caller rate limit for the target extension.
    /// </summary>
    /// <param name="callerId">The ID of the calling extension.</param>
    /// <param name="targetExtensionId">The ID of the target extension being invoked.</param>
    /// <param name="rateLimitPerCaller">The maximum number of invocations allowed per caller per minute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="RateLimitExceededException">Thrown when the rate limit is exceeded.</exception>
    public async Task ValidateRateLimitAsync(
        string callerId,
        string targetExtensionId,
        int rateLimitPerCaller,
        CancellationToken cancellationToken)
    {
        if (rateLimitPerCaller <= 0) return; // No rate limit configured

        var key = $"ext-invoke:{targetExtensionId}:caller:{callerId}";
        var window = TimeSpan.FromMinutes(1); // Per-minute rate limit

        var result = await _rateLimiter.CheckAsync(key, rateLimitPerCaller, window, cancellationToken);
        if (!result.IsAllowed)
        {
            throw new RateLimitExceededException(targetExtensionId, callerId);
        }
    }
}
