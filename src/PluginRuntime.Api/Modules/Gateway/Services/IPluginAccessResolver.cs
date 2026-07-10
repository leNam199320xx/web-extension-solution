namespace PluginRuntime.Api.Modules.Gateway.Services;

/// <summary>
/// Resolves and manages the materialized plugin access set for tenants.
/// Access is the union of free (publicly accessible) plugins and package subscription plugins.
/// </summary>
public interface IPluginAccessResolver
{
    /// <summary>
    /// Gets the current set of accessible plugin IDs for a tenant.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetAccessiblePluginsAsync(Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Recalculates and persists the full access set for a specific tenant.
    /// Called when subscriptions change (subscribe/unsubscribe).
    /// </summary>
    Task RecalculateAccessAsync(Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Recalculates access for all tenants subscribed to a specific package.
    /// Called when package composition changes (plugins added/removed).
    /// </summary>
    Task RecalculateForPackageAsync(Guid packageId, CancellationToken ct);
}
