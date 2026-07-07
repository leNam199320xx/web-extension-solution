namespace PluginRuntime.Core.Exceptions;

/// <summary>
/// Thrown when an inter-extension invocation exceeds the configured maximum call depth.
/// Default maximum is 3 levels deep.
/// </summary>
public class MaxCallDepthExceededException : PluginRuntimeException
{
    public int MaxDepth { get; }
    public int CurrentDepth { get; }

    public MaxCallDepthExceededException(int maxDepth, int currentDepth)
        : base(
            "MAX_CALL_DEPTH_EXCEEDED",
            "Execution",
            $"Maximum call depth of {maxDepth} exceeded (current depth: {currentDepth}).")
    {
        MaxDepth = maxDepth;
        CurrentDepth = currentDepth;
    }
}
