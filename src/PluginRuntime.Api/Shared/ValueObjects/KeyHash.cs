using System.Security.Cryptography;
using System.Text;
using PluginRuntime.Api.Shared.Exceptions;

namespace PluginRuntime.Api.Shared.ValueObjects;

/// <summary>
/// KeyHash value object that computes and stores a SHA-256 hash (hex lowercase) from a plaintext key.
/// </summary>
public sealed record KeyHash
{
    public string Value { get; }

    public KeyHash(string plaintextKey)
    {
        if (string.IsNullOrWhiteSpace(plaintextKey))
            throw new DomainException("Plaintext key cannot be empty.");

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintextKey));
        Value = Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Creates a KeyHash from a pre-computed hash value (for hydration from persistence).
    /// </summary>
    private KeyHash(string hashValue, bool fromStorage)
    {
        Value = hashValue;
    }

    /// <summary>
    /// Reconstructs a KeyHash from a stored hash value without re-hashing.
    /// </summary>
    public static KeyHash FromStoredHash(string hashValue)
    {
        if (string.IsNullOrWhiteSpace(hashValue))
            throw new DomainException("Stored hash value cannot be empty.");

        return new KeyHash(hashValue, fromStorage: true);
    }

    public override string ToString() => Value;
}
