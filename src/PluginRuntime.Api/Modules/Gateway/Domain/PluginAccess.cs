namespace PluginRuntime.Api.Modules.Gateway.Domain;

/// <summary>
/// Materialized access entry: maps a tenant to an accessible plugin.
/// The composite key is (TenantId, PluginId).
/// </summary>
public sealed class PluginAccess
{
    public Guid TenantId { get; private set; }
    public Guid PluginId { get; private set; }
    public AccessSource Source { get; private set; }
    public Guid? PackageId { get; private set; }
    public DateTime GrantedAt { get; private set; }

    private PluginAccess() { }

    public static PluginAccess Create(Guid tenantId, Guid pluginId, AccessSource source, Guid? packageId = null)
    {
        return new PluginAccess
        {
            TenantId = tenantId,
            PluginId = pluginId,
            Source = source,
            PackageId = packageId,
            GrantedAt = DateTime.UtcNow
        };
    }
}

public enum AccessSource
{
    Free,
    Package
}
