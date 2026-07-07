using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PluginRuntime.Core.Enums;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Audit;

public class AuditLoggerService : IAuditLogger
{
    private const string MeterName = "PluginRuntime.Metrics";
    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> SignatureFailures = Meter.CreateCounter<long>(
        "security_signature_failures_total", description: "Total signature verification failures");

    private static readonly Counter<long> CapabilityDenied = Meter.CreateCounter<long>(
        "security_capability_denied_total", description: "Total capability access denials");

    private static readonly Counter<long> RevokedAttempts = Meter.CreateCounter<long>(
        "security_revoked_execution_attempts", description: "Total attempts to execute revoked plugins");

    private static readonly Counter<long> TimeoutTotal = Meter.CreateCounter<long>(
        "plugin_timeout_total", description: "Total plugin execution timeouts");

    private readonly PluginRuntimeDbContext _dbContext;
    private readonly ILogger<AuditLoggerService> _logger;

    public AuditLoggerService(PluginRuntimeDbContext dbContext, ILogger<AuditLoggerService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var auditLog = new AuditLogEntity
        {
            AuditId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            ActorId = entry.ActorId,
            ActorType = Enum.TryParse<ActorType>(entry.ActorType, out var actorType) ? actorType : ActorType.System,
            Action = entry.Action,
            ResourceType = entry.ResourceType,
            ResourceId = entry.ResourceId,
            IpAddress = entry.IpAddress,
            Result = Enum.TryParse<AuditResult>(entry.Result, out var result) ? result : AuditResult.Failure,
            Metadata = entry.Metadata != null ? JsonSerializer.Serialize(entry.Metadata) : null
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        IncrementSecurityMetric(entry.Action);

        _logger.LogInformation(
            "Audit: {Action} on {ResourceType}/{ResourceId} by {ActorId} - Result: {Result}",
            entry.Action, entry.ResourceType, entry.ResourceId, entry.ActorId, entry.Result);
    }

    private static void IncrementSecurityMetric(string action)
    {
        switch (action.ToLowerInvariant())
        {
            case "invalid_signature":
            case "signatureverificationfailed":
            case "pipeline.signatureverifier.failed":
                SignatureFailures.Add(1);
                break;

            case "hash_mismatch":
            case "hashverificationfailed":
            case "pipeline.hashverifier.failed":
                SignatureFailures.Add(1);
                break;

            case "capability_violation":
            case "capability_denied":
            case "capabilitydenied":
                CapabilityDenied.Add(1);
                break;

            case "timeout_exceeded":
            case "executiontimeout":
                TimeoutTotal.Add(1);
                break;

            case "revoked_plugin_attempt":
            case "revokedpluginattempt":
            case "pipeline.revocationchecker.failed":
                RevokedAttempts.Add(1);
                break;
        }
    }
}
