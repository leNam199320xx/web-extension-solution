namespace PluginRuntime.Capabilities.Abstractions;

public record NetworkResponse
{
    public int StatusCode { get; init; }
    public string Body { get; init; } = "";
    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>();
}
