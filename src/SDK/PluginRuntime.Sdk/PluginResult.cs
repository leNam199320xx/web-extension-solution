using System.Text.Json;

namespace PluginRuntime.Sdk;

public record PluginResult
{
    public bool Success { get; init; }
    public JsonElement? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
