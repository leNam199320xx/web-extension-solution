namespace PluginRuntime.Core.Exceptions;

/// <summary>
/// Thrown when a caller exceeds the per-caller rate limit defined in the target extension's invocation_policy.
/// </summary>
public class RateLimitExceededException : PluginRuntimeException
{
    public string TargetExtensionId { get; }
    public string CallerId { get; }

    public RateLimitExceededException(string targetExtensionId, string callerId)
        : base("RATE_LIMIT_EXCEEDED", "ResourceLimit",
            $"Rate limit exceeded for caller '{callerId}' invoking extension '{targetExtensionId}'.")
    {
        TargetExtensionId = targetExtensionId;
        CallerId = callerId;
    }
}
