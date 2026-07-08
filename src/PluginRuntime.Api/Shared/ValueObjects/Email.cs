using System.Text.RegularExpressions;
using PluginRuntime.Api.Shared.Exceptions;

namespace PluginRuntime.Api.Shared.ValueObjects;

/// <summary>
/// Email value object with RFC 5322 validation and lowercase normalization.
/// </summary>
public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250));

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email address cannot be empty.");

        if (value.Length > 254)
            throw new DomainException("Email address cannot exceed 254 characters.");

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
            throw new DomainException($"'{value}' is not a valid email address (RFC 5322).");

        Value = normalized;
    }

    public override string ToString() => Value;
}
