namespace PluginRuntime.Capabilities.Abstractions;

public record StorageMetadata
{
    public string? ContentType { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public TimeSpan? Expiration { get; init; }
}
