using PluginRuntime.Core.Enums;

namespace PluginRuntime.Core.Interfaces;

public record AuditLogRecord(
    Guid AuditId,
    DateTime Timestamp,
    string ActorId,
    ActorType ActorType,
    string Action,
    string ResourceType,
    string ResourceId,
    string? IpAddress,
    AuditResult Result,
    string? Metadata);

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogRecord auditLog, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditLogRecord>> GetByResourceAsync(string resourceType, string resourceId, CancellationToken cancellationToken);
}
