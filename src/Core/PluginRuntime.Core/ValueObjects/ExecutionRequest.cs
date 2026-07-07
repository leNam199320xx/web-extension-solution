using System.Text.Json;

namespace PluginRuntime.Core.ValueObjects;

public record ExecutionRequest(
    Guid PluginId,
    string? Version,
    JsonElement Input,
    string? CorrelationId,
    string? UserId,
    string? TenantId);
