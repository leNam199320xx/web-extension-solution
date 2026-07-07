namespace PluginRuntime.Core.ValueObjects;

public record VerificationResult(bool IsValid, string? ErrorCode, string? ErrorMessage);
