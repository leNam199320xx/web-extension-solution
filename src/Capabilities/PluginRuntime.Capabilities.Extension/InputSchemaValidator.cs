using System.Text.Json;

namespace PluginRuntime.Capabilities.Extension;

/// <summary>
/// Validates caller's input against target's invocation_policy.allowed_input_schema (JSON Schema).
/// Rejects invalid input without executing the target extension.
/// </summary>
public static class InputSchemaValidator
{
    /// <summary>
    /// Validates input against a JSON Schema definition.
    /// Checks type constraints and required properties.
    /// </summary>
    /// <param name="input">The input JSON element to validate.</param>
    /// <param name="allowedInputSchema">The JSON Schema defining allowed input structure.</param>
    /// <returns>True if the input is valid against the schema, false otherwise.</returns>
    public static bool Validate(JsonElement input, JsonElement? allowedInputSchema)
    {
        if (allowedInputSchema is null || allowedInputSchema.Value.ValueKind == JsonValueKind.Undefined)
            return true; // No schema defined, allow all input

        if (allowedInputSchema.Value.ValueKind == JsonValueKind.Null)
            return true;

        // Check type constraint
        if (allowedInputSchema.Value.TryGetProperty("type", out var type))
        {
            var expectedType = type.GetString();
            var actualKind = input.ValueKind;

            var valid = expectedType switch
            {
                "object" => actualKind == JsonValueKind.Object,
                "array" => actualKind == JsonValueKind.Array,
                "string" => actualKind == JsonValueKind.String,
                "number" or "integer" => actualKind == JsonValueKind.Number,
                "boolean" => actualKind == JsonValueKind.True || actualKind == JsonValueKind.False,
                "null" => actualKind == JsonValueKind.Null,
                _ => true
            };

            if (!valid) return false;
        }

        // Check required properties if schema defines them
        if (allowedInputSchema.Value.TryGetProperty("required", out var required) &&
            required.ValueKind == JsonValueKind.Array)
        {
            if (input.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var prop in required.EnumerateArray())
            {
                var propName = prop.GetString();
                if (propName is not null && !input.TryGetProperty(propName, out _))
                    return false;
            }
        }

        return true;
    }
}
