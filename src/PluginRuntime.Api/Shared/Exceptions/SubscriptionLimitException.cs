namespace PluginRuntime.Api.Shared.Exceptions;

/// <summary>
/// Thrown when a subscription operation exceeds the tenant's plan limits.
/// </summary>
public sealed class SubscriptionLimitException : UnifiedApiException
{
    public SubscriptionLimitException(string code, string message)
        : base(code, 403, message)
    {
    }
}
