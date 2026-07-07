namespace PluginRuntime.Capabilities.Extension;

/// <summary>
/// Maintains the call stack for inter-extension invocations.
/// Used to detect circular invocations and enforce max call depth.
/// Thread-safe via AsyncLocal for per-execution-flow tracking.
/// </summary>
public sealed class CallStack
{
    private static readonly AsyncLocal<Stack<string>> _callStack = new();

    /// <summary>
    /// Gets the current call stack for the executing async flow.
    /// </summary>
    public static Stack<string> Current => _callStack.Value ??= new Stack<string>();

    /// <summary>
    /// Gets the current call depth (number of extensions in the call chain).
    /// </summary>
    public static int Depth => Current.Count;

    /// <summary>
    /// Pushes an extension ID onto the call stack before invoking it.
    /// </summary>
    public static void Push(string extensionId)
    {
        Current.Push(extensionId);
    }

    /// <summary>
    /// Pops the top extension ID from the call stack after invocation completes.
    /// </summary>
    public static void Pop()
    {
        if (Current.Count > 0)
            Current.Pop();
    }

    /// <summary>
    /// Checks if an extension ID is already in the call stack (circular invocation detection).
    /// </summary>
    public static bool Contains(string extensionId) => Current.Contains(extensionId);

    /// <summary>
    /// Clears the call stack for the current async flow.
    /// </summary>
    public static void Clear()
    {
        _callStack.Value = new Stack<string>();
    }
}
