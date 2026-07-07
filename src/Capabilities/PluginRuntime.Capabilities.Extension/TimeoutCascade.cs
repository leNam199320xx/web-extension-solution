namespace PluginRuntime.Capabilities.Extension;

/// <summary>
/// Calculates cascading timeout for inter-extension invocations.
/// Child timeout = min(target's manifest timeout_ms, caller's remaining time).
/// </summary>
public static class TimeoutCascade
{
    /// <summary>
    /// Calculates the effective timeout for a child extension invocation.
    /// The child gets the lesser of the target's declared timeout and the caller's remaining time.
    /// </summary>
    /// <param name="targetTimeoutMs">The target extension's manifest-defined timeout in milliseconds.</param>
    /// <param name="callerRemainingTime">The caller's remaining execution time.</param>
    /// <returns>The effective timeout for the child invocation.</returns>
    public static TimeSpan Calculate(int targetTimeoutMs, TimeSpan callerRemainingTime)
    {
        var targetTimeout = TimeSpan.FromMilliseconds(targetTimeoutMs);
        return callerRemainingTime < targetTimeout ? callerRemainingTime : targetTimeout;
    }
}
