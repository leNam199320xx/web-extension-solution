using PluginRuntime.Core.Enums;

namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class AuditLogEntity
{
    public Guid AuditId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ActorId { get; set; } = "";
    public ActorType ActorType { get; set; }
    public string Action { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public string ResourceId { get; set; } = "";
    public string? IpAddress { get; set; }
    public AuditResult Result { get; set; }
    public string? Metadata { get; set; }
}
