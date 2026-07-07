namespace PluginRuntime.Core.Exceptions;

/// <summary>
/// Thrown when a target extension does not exist or is not Active.
/// </summary>
public class ExtensionNotFoundException : PluginRuntimeException
{
    public string ExtensionId { get; }

    public ExtensionNotFoundException(string extensionId)
        : base(
            "EXTENSION_NOT_FOUND",
            "NotFound",
            $"Extension '{extensionId}' was not found or is not active.")
    {
        ExtensionId = extensionId;
    }
}
