using PluginRuntime.Core.Enums;

namespace PluginRuntime.Infrastructure.Persistence.Entities;

public class PermissionReviewEntity
{
    public Guid ReviewId { get; set; }
    public Guid VersionId { get; set; }
    public string Permissions { get; set; } = "";
    public string RiskSummary { get; set; } = "";
    public string? PermissionDiff { get; set; }
    public RiskLevel OverallRiskLevel { get; set; }
    public Guid? ReviewerId { get; set; }
    public ApprovalDecision? Decision { get; set; }
    public string? Comment { get; set; }
    public string? Conditions { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
