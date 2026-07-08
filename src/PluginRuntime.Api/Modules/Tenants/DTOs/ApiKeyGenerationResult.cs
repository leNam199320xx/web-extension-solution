namespace PluginRuntime.Api.Modules.Tenants.DTOs;

/// <summary>
/// Result returned when a new API key is generated.
/// Contains the plaintext key shown exactly once to the user.
/// </summary>
public sealed record ApiKeyGenerationResult
{
    /// <summary>The unique identifier of the created API key.</summary>
    public Guid KeyId { get; init; }

    /// <summary>
    /// The full plaintext API key. This value is returned exactly once at creation time
    /// and cannot be retrieved again.
    /// </summary>
    public string PlaintextKey { get; init; } = string.Empty;

    /// <summary>The first 8 characters of the key for identification.</summary>
    public string Prefix { get; init; } = string.Empty;

    /// <summary>Optional expiration date for the key.</summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>When the key was created.</summary>
    public DateTime CreatedAt { get; init; }
}
