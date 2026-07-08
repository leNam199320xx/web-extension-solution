namespace PluginRuntime.Api.Modules.Tenants.Domain;

/// <summary>
/// Immutable audit log entry recording tenant lifecycle transitions and plan changes.
/// Once created, entries cannot be modified or deleted.
/// </summary>
public sealed class AuditLogEntry
{
    public Guid EntryId { get; private set; }
    public Guid? TenantId { get; private set; }
    public string ActorId { get; private set; } = string.Empty;
    public string ActionType { get; private set; } = string.Empty;
    public string TargetEntity { get; private set; } = string.Empty;
    public string? PreviousState { get; private set; }
    public string? NewState { get; private set; }
    public string? Reason { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AuditLogEntry() { }

    public static AuditLogEntry Create(
        Guid? tenantId,
        string actorId,
        string actionType,
        string targetEntity,
        string? previousState,
        string? newState,
        string? reason)
    {
        return new AuditLogEntry
        {
            EntryId = Guid.NewGuid(),
            TenantId = tenantId,
            ActorId = actorId,
            ActionType = actionType,
            TargetEntity = targetEntity,
            PreviousState = previousState,
            NewState = newState,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };
    }
}
