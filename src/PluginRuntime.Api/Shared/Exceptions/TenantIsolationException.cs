namespace PluginRuntime.Api.Shared.Exceptions;

/// <summary>
/// Thrown when a tenant attempts to access resources belonging to another tenant.
/// </summary>
public sealed class TenantIsolationException : UnifiedApiException
{
    public TenantIsolationException()
        : base("UA-AUTH-001", 403, "Access denied")
    {
    }
}
