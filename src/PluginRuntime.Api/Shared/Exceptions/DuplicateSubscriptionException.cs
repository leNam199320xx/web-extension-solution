namespace PluginRuntime.Api.Shared.Exceptions;

/// <summary>
/// Thrown when a tenant attempts to subscribe to a package they already subscribe to.
/// </summary>
public sealed class DuplicateSubscriptionException : UnifiedApiException
{
    public DuplicateSubscriptionException()
        : base("UA-SUB-002", 409, "Already subscribed to this package")
    {
    }
}
