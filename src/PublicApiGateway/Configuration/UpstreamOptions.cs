namespace PublicApiGateway.Configuration;

/// <summary>
/// Upstream (PluginRuntime.Api) connection configuration.
/// </summary>
public sealed class UpstreamOptions
{
    public const string SectionName = "Upstream";

    public string BaseUrl { get; set; } = "http://localhost:5000";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxTimeoutSeconds { get; set; } = 300;
}
