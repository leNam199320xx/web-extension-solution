namespace PluginRuntime.Api.Shared.Exceptions;

/// <summary>
/// Thrown when a non-admin user attempts to perform platform admin operations.
/// </summary>
public sealed class InternalTenantAuthException : UnifiedApiException
{
    public InternalTenantAuthException()
        : base("UA-INT-001", 403, "Platform admin role required")
    {
    }
}
