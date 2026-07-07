using System.Text.Json;

namespace PluginRuntime.Core.ValueObjects;

public record ExecutionResult(
    bool Success,
    JsonElement? Data,
    string ExecutionId,
    string TraceId,
    int DurationMs,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? ErrorCategory = null,
    string? FailingStage = null);
