namespace PublicApiGateway.Models;

/// <summary>
/// Base exception for all gateway errors. Maps to structured error responses.
/// </summary>
public abstract class GatewayException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    protected GatewayException(string errorCode, int httpStatusCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}

public sealed class AuthenticationRequiredException : GatewayException
{
    public AuthenticationRequiredException(string code = ErrorCodes.AuthRequired, string message = "API key required")
        : base(code, 401, message) { }
}

public sealed class AccessDeniedException : GatewayException
{
    public AccessDeniedException(string code = ErrorCodes.AuthRevoked, string message = "Access denied")
        : base(code, 403, message) { }
}

public sealed class RateLimitExceededException : GatewayException
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds = 60)
        : base(ErrorCodes.RateLimitExceeded, 429, "Rate limit exceeded")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

public sealed class QuotaExceededException : GatewayException
{
    public int RetryAfterSeconds { get; }

    public QuotaExceededException(int retryAfterSeconds)
        : base(ErrorCodes.QuotaExceeded, 429, "Daily quota exceeded")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

public sealed class ServiceUnavailableException : GatewayException
{
    public ServiceUnavailableException(string code, string message = "Service unavailable")
        : base(code, 503, message) { }
}

public sealed class UpstreamException : GatewayException
{
    public UpstreamException(string code = ErrorCodes.UpstreamError, string message = "Upstream service error")
        : base(code, 502, message) { }
}

public sealed class SecurityViolationException : GatewayException
{
    public SecurityViolationException(string code, int statusCode, string message)
        : base(code, statusCode, message) { }
}

/// <summary>
/// All GW- error code constants.
/// </summary>
public static class ErrorCodes
{
    // Authentication
    public const string AuthRequired = "GW-AUTH-001";
    public const string AuthExpired = "GW-AUTH-002";
    public const string AuthRevoked = "GW-AUTH-003";

    // Rate Limiting
    public const string RateLimitExceeded = "GW-RATE-001";
    public const string RateLimitUnavailable = "GW-RATE-002";

    // Quota
    public const string QuotaExceeded = "GW-QUOTA-001";
    public const string QuotaUnavailable = "GW-QUOTA-002";

    // Upstream
    public const string UpstreamError = "GW-UPSTREAM-001";
    public const string UpstreamNonConforming = "GW-UPSTREAM-002";

    // Security
    public const string BodyTooLarge = "GW-SEC-001";
    public const string HeaderTooLarge = "GW-SEC-002";
    public const string HttpsRequired = "GW-SEC-004";
    public const string InvalidKeyFormat = "GW-SEC-005";
}
