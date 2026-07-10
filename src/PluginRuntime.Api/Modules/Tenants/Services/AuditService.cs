using PluginRuntime.Api.Modules.Tenants.Domain;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// Persists immutable audit log entries using IRepository.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly IRepository<AuditLogEntry> _auditLogs;

    public AuditService(IRepository<AuditLogEntry> auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task LogAsync(
        Guid? tenantId,
        string actorId,
        string actionType,
        string targetEntity,
        string? previousState,
        string? newState,
        string? reason,
        CancellationToken ct)
    {
        var entry = AuditLogEntry.Create(
            tenantId, actorId, actionType, targetEntity, previousState, newState, reason);

        await _auditLogs.AddAsync(entry, ct);
        await _auditLogs.SaveChangesAsync(ct);
    }
}
