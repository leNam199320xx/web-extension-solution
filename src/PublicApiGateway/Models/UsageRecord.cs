namespace PublicApiGateway.Models;

/// <summary>
/// Captured usage record for async metering to PostgreSQL.
/// </summary>
public sealed record UsageRecord(
    Guid RecordId,
    Guid TenantId,
    string Method,
    string Path,
    int StatusCode,
    long DurationMs,
    long RequestBodyBytes,
    long ResponseBodyBytes,
    string CorrelationId,
    DateTime Timestamp);
