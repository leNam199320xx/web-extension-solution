namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// Records immutable audit log entries for tenant lifecycle transitions and plan changes.
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        Guid? tenantId,
        string actorId,
        string actionType,
        string targetEntity,
        string? previousState,
        string? newState,
        string? reason,
        CancellationToken ct);
}
