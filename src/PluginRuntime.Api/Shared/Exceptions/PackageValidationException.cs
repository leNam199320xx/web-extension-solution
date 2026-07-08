namespace PluginRuntime.Api.Shared.Exceptions;

/// <summary>
/// Thrown when a plugin package contains invalid or inactive plugin IDs.
/// </summary>
public sealed class PackageValidationException : UnifiedApiException
{
    public PackageValidationException(string message)
        : base("UA-PKG-001", 400, message)
    {
    }
}
