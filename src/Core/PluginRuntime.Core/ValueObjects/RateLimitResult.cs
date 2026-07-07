namespace PluginRuntime.Core.ValueObjects;

public record RateLimitResult(bool IsAllowed, int Remaining, TimeSpan RetryAfter);
