using PluginRuntime.Core.Enums;

namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class ApprovalEntity
{
    public Guid ApprovalId { get; set; }
    public Guid VersionId { get; set; }
    public Guid ReviewerId { get; set; }
    public ApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTime DecidedAt { get; set; }
}
