namespace PluginRuntime.Core.Exceptions;

/// <summary>
/// Thrown when a circular invocation is detected in the inter-extension call chain.
/// For example, extension A calls B which calls A again.
/// </summary>
public class CircularInvocationException : PluginRuntimeException
{
    public string CallerExtensionId { get; }
    public string TargetExtensionId { get; }

    public CircularInvocationException(string callerExtensionId, string targetExtensionId)
        : base(
            "CIRCULAR_INVOCATION",
            "Execution",
            $"Circular invocation detected: '{callerExtensionId}' -> '{targetExtensionId}' forms a cycle.")
    {
        CallerExtensionId = callerExtensionId;
        TargetExtensionId = targetExtensionId;
    }
}
