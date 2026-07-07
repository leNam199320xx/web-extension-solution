namespace PluginRuntime.Capabilities.Abstractions;

public record NetworkRequest
{
    public string Url { get; init; } = "";
    public string Method { get; init; } = "GET";
    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>();
    public string? Body { get; init; }
    public int TimeoutMs { get; init; } = 5000;
}
