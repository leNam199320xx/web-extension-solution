namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class DeclarativeConfigEntity
{
    public Guid ConfigId { get; set; }
    public string ExtensionId { get; set; } = "";
    public string Version { get; set; } = "";
    public string Config { get; set; } = "";
    public string? InputSchema { get; set; }
    public string? OutputSchema { get; set; }
    public DateTime CreatedAt { get; set; }
}
