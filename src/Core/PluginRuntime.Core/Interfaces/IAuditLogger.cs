namespace PluginRuntime.Core.Interfaces;

public record AuditEntry(
    string TraceId,
    string ActorId,
    string ActorType,
    string Action,
    string ResourceType,
    string ResourceId,
    string Result,
    string? IpAddress,
    Dictionary<string, object>? Metadata);

public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken);
}
