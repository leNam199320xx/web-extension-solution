using PluginRuntime.Core.Enums;

namespace PluginRuntime.Core.Entities;

public class Execution
{
    public Guid ExecutionId { get; init; }
    public Guid PluginId { get; init; }
    public Guid VersionId { get; init; }
    public string TraceId { get; init; }
    public string? CorrelationId { get; init; }
    public string? TenantId { get; init; }
    public string? UserId { get; init; }
    public ExecutionStatus Status { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public int? DurationMs { get; init; }
    public string? NodeId { get; init; }

    public Execution(
        Guid executionId,
        Guid pluginId,
        Guid versionId,
        string traceId,
        ExecutionStatus status = ExecutionStatus.Running,
        string? correlationId = null,
        string? tenantId = null,
        string? userId = null,
        string? errorCode = null,
        string? errorMessage = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? durationMs = null,
        string? nodeId = null)
    {
        if (executionId == Guid.Empty)
            throw new ArgumentException("ExecutionId must not be empty.", nameof(executionId));
        if (pluginId == Guid.Empty)
            throw new ArgumentException("PluginId must not be empty.", nameof(pluginId));
        if (versionId == Guid.Empty)
            throw new ArgumentException("VersionId must not be empty.", nameof(versionId));
        if (string.IsNullOrWhiteSpace(traceId))
            throw new ArgumentException("TraceId must not be null or empty.", nameof(traceId));

        ExecutionId = executionId;
        PluginId = pluginId;
        VersionId = versionId;
        TraceId = traceId;
        CorrelationId = correlationId;
        TenantId = tenantId;
        UserId = userId;
        Status = status;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        StartTime = startTime ?? DateTime.UtcNow;
        EndTime = endTime;
        DurationMs = durationMs;
        NodeId = nodeId;
    }
}
