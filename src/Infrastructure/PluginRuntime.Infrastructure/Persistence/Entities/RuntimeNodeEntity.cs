namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class RuntimeNodeEntity
{
    public string NodeId { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string Version { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTime StartedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
}
