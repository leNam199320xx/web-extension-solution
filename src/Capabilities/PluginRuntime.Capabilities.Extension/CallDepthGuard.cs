using PluginRuntime.Core.Exceptions;

namespace PluginRuntime.Capabilities.Extension;

/// <summary>
/// Enforces call depth limit and circular invocation detection.
/// Must be called before each inter-extension invoke.
/// </summary>
public static class CallDepthGuard
{
    /// <summary>
    /// Default maximum call depth for inter-extension invocations.
    /// Configurable via InterExtension.MaxCallDepth setting.
    /// </summary>
    public const int DefaultMaxCallDepth = 3;

    /// <summary>
    /// Validates that the invocation does not exceed the maximum call depth
    /// and does not form a circular invocation chain.
    /// </summary>
    /// <param name="callerExtensionId">The extension initiating the call.</param>
    /// <param name="targetExtensionId">The extension being invoked.</param>
    /// <param name="maxCallDepth">The configurable maximum call depth (default: 3).</param>
    /// <exception cref="MaxCallDepthExceededException">Thrown when call depth exceeds the maximum.</exception>
    /// <exception cref="CircularInvocationException">Thrown when a circular invocation is detected.</exception>
    public static void Validate(string callerExtensionId, string targetExtensionId, int maxCallDepth = DefaultMaxCallDepth)
    {
        // Check max depth first
        if (CallStack.Depth >= maxCallDepth)
        {
            throw new MaxCallDepthExceededException(maxCallDepth, CallStack.Depth);
        }

        // Check circular invocation
        if (CallStack.Contains(targetExtensionId))
        {
            throw new CircularInvocationException(callerExtensionId, targetExtensionId);
        }
    }
}
