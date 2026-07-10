namespace PublicApiGateway.Configuration;

/// <summary>
/// Gateway-level configuration: cache TTL, size limits, buffer capacity, IP blocking, key format.
/// </summary>
public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public int CacheTtlSeconds { get; set; } = 300;
    public long MaxRequestBodyBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public int MaxRequestHeaderBytes { get; set; } = 8192;
    public int UsageBufferCapacity { get; set; } = 10_000;
    public int IpBlockThreshold { get; set; } = 10;
    public int IpBlockWindowSeconds { get; set; } = 60;
    public int IpBlockDurationSeconds { get; set; } = 300;
    public string ApiKeyPattern { get; set; } = @"^[a-zA-Z0-9\-_]{32,128}$";
}
