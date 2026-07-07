namespace PluginRuntime.Core.ValueObjects;

public record ValidationResult(bool IsValid, IReadOnlyList<ValidationError> Errors);

public record ValidationError(string Field, string Code, string Message);
