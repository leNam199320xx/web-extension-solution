namespace PublicApiGateway.Models;

/// <summary>
/// Structured error response returned by the gateway.
/// </summary>
public sealed record GatewayError(
    string Code,
    string Message,
    string TraceId,
    string Timestamp);
