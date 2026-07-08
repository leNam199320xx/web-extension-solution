using PluginRuntime.Api.Modules.Tenants.Domain;
using PluginRuntime.Api.Shared.Infrastructure;

namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// Persists immutable audit log entries to the database.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
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
            tenantId,
            actorId,
            actionType,
            targetEntity,
            previousState,
            newState,
            reason);

        _db.AuditLogEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
