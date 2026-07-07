namespace PluginRuntime.Core.Exceptions;

/// <summary>
/// Thrown when visibility rules deny access to an extension.
/// Private extensions allow same-owner only, Subscription extensions require an active approved subscription.
/// </summary>
public class AccessDeniedException : PluginRuntimeException
{
    public string ExtensionId { get; }
    public Guid CallerId { get; }

    public AccessDeniedException(string extensionId, Guid callerId)
        : base(
            "ACCESS_DENIED",
            "Security",
            $"Access denied to extension '{extensionId}' for caller '{callerId}'.")
    {
        ExtensionId = extensionId;
        CallerId = callerId;
    }
}
