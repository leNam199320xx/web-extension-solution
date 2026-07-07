namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class CapabilityEntity
{
    public Guid CapabilityId { get; set; }
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0";
    public string Category { get; set; } = "";
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
