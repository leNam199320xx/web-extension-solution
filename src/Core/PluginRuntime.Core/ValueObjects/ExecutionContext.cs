namespace PluginRuntime.Core.ValueObjects;

public record ExecutionContext(
    string ExecutionId,
    Guid PluginId,
    string? Version,
    string? CorrelationId,
    string? UserId,
    string? TenantId);
