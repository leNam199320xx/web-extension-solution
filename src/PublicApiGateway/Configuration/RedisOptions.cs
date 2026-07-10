namespace PublicApiGateway.Configuration;

/// <summary>
/// Redis connection configuration.
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "ConnectionStrings";

    public string Redis { get; set; } = "localhost:6379";
}
