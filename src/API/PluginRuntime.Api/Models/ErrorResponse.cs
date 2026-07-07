namespace PluginRuntime.Api.Models;

/// <summary>
/// Standardized error response envelope returned by all API error responses.
/// </summary>
public record ErrorResponse(ErrorDetail Error);

/// <summary>
/// Error detail containing code, category, message, trace ID, and timestamp.
/// </summary>
public record ErrorDetail(string Code, string Category, string Message, string TraceId, string Timestamp);
