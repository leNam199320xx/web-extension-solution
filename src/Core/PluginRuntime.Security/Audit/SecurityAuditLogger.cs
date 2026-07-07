using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Security.Audit;

/// <summary>
/// Logs security-related validation failures as immutable audit entries.
/// Used by the execution pipeline to record any security rejection.
/// </summary>
public class SecurityAuditLogger
{
    private readonly IAuditLogger _auditLogger;

    public SecurityAuditLogger(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    public async Task LogValidationFailureAsync(
        string traceId,
        Guid pluginId,
        string failureReason,
        string failureCode,
        string? actorId = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            TraceId: traceId,
            ActorId: actorId ?? "system",
            ActorType: "System",
            Action: "SecurityValidationFailed",
            ResourceType: "Plugin",
            ResourceId: pluginId.ToString(),
            Result: "Denied",
            IpAddress: null,
            Metadata: new Dictionary<string, object>
            {
                ["failure_reason"] = failureReason,
                ["failure_code"] = failureCode,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            });

        await _auditLogger.LogAsync(entry, cancellationToken);
    }

    public async Task LogSignatureFailureAsync(
        string traceId,
        Guid pluginId,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            TraceId: traceId,
            ActorId: "system",
            ActorType: "System",
            Action: "SignatureVerificationFailed",
            ResourceType: "Plugin",
            ResourceId: pluginId.ToString(),
            Result: "Denied",
            IpAddress: null,
            Metadata: new Dictionary<string, object>
            {
                ["error_code"] = errorCode,
                ["error_message"] = errorMessage,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            });

        await _auditLogger.LogAsync(entry, cancellationToken);
    }

    public async Task LogHashFailureAsync(
        string traceId,
        Guid pluginId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            TraceId: traceId,
            ActorId: "system",
            ActorType: "System",
            Action: "HashVerificationFailed",
            ResourceType: "Plugin",
            ResourceId: pluginId.ToString(),
            Result: "Denied",
            IpAddress: null,
            Metadata: new Dictionary<string, object>
            {
                ["error_code"] = "HASH_MISMATCH",
                ["error_message"] = errorMessage,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            });

        await _auditLogger.LogAsync(entry, cancellationToken);
    }

    public async Task LogRevocationFailureAsync(
        string traceId,
        Guid pluginId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry(
            TraceId: traceId,
            ActorId: "system",
            ActorType: "System",
            Action: "RevokedPluginAttempt",
            ResourceType: "PluginVersion",
            ResourceId: versionId.ToString(),
            Result: "Denied",
            IpAddress: null,
            Metadata: new Dictionary<string, object>
            {
                ["plugin_id"] = pluginId.ToString(),
                ["error_code"] = "PLUGIN_REVOKED",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            });

        await _auditLogger.LogAsync(entry, cancellationToken);
    }
}
