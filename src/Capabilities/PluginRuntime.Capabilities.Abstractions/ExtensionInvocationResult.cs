using System.Text.Json;

namespace PluginRuntime.Capabilities.Abstractions;

public record ExtensionInvocationResult
{
    public bool Success { get; init; }
    public JsonElement? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string TargetExecutionId { get; init; } = "";
    public int DurationMs { get; init; }
}
