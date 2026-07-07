using System.Collections.Generic;
using System.Text.Json;

namespace PluginRuntime.Sdk;

public record PluginContext
{
    public string ExecutionId { get; init; } = "";
    public string PluginId { get; init; } = "";
    public string Version { get; init; } = "";
    public JsonElement Input { get; init; }
    public IReadOnlyDictionary<string, object> Capabilities { get; init; }
        = new Dictionary<string, object>();
    public string? CorrelationId { get; init; }
}
