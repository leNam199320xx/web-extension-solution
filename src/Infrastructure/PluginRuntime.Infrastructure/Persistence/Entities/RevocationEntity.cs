namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class RevocationEntity
{
    public Guid RevocationId { get; set; }
    public Guid VersionId { get; set; }
    public string Reason { get; set; } = "";
    public Guid RevokedBy { get; set; }
    public DateTime RevokedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
