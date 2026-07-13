namespace PluginRuntime.Api.Shared.Entities;

/// <summary>
/// Precomputed ecosystem stats (cached/aggregated).
/// </summary>
public sealed class EcosystemStats
{
    public Guid Id { get; set; }
    public int TotalExtensions { get; set; }
    public int TotalPublishers { get; set; }
    public int TotalSubscriptions { get; set; }
    public DateTime ComputedAt { get; set; }
}
