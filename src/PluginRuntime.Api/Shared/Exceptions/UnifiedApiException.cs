namespace PluginRuntime.Api.Shared.Exceptions;

/// <summary>
/// Base exception for all API-level business errors in the Unified API.
/// Each derived exception maps to a specific error code and HTTP status.
/// </summary>
public abstract class UnifiedApiException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    protected UnifiedApiException(string code, int statusCode, string message)
        : base(message)
    {
        ErrorCode = code;
        HttpStatusCode = statusCode;
    }
}
