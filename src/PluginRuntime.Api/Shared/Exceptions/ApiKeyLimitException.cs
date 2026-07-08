namespace PluginRuntime.Api.Shared.Exceptions;

/// <summary>
/// Thrown when a tenant attempts to create more API keys than their plan allows.
/// </summary>
public sealed class ApiKeyLimitException : UnifiedApiException
{
    public ApiKeyLimitException()
        : base("UA-KEY-001", 403, "Maximum API keys reached for current plan")
    {
    }
}
