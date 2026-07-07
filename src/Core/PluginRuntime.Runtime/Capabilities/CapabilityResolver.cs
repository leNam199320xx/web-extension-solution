using System.Text.Json;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Exceptions;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;

using ExecutionContext = PluginRuntime.Core.ValueObjects.ExecutionContext;

namespace PluginRuntime.Runtime.Capabilities;

/// <summary>
/// Resolves capabilities for a plugin based on its manifest declarations.
/// Implements deny-by-default: only capabilities explicitly declared in the manifest are granted.
/// Undeclared capability access is immediately denied and logged as a security event.
/// </summary>
public class CapabilityResolver : ICapabilityResolver
{
    private readonly IReadOnlyDictionary<string, Func<Guid, ICapability>> _capabilityFactories;
    private readonly IAuditLogger _auditLogger;

    public CapabilityResolver(
        IReadOnlyDictionary<string, Func<Guid, ICapability>> capabilityFactories,
        IAuditLogger auditLogger)
    {
        _capabilityFactories = capabilityFactories ?? throw new ArgumentNullException(nameof(capabilityFactories));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    /// <summary>
    /// Resolves ONLY capabilities explicitly granted in the manifest.
    /// Returns a dictionary keyed by capability name containing only declared capabilities.
    /// No implicit permissions are granted.
    /// </summary>
    public IReadOnlyDictionary<string, ICapability> Resolve(Manifest manifest, ExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(executionContext);

        var grantedCapabilities = new Dictionary<string, ICapability>(StringComparer.OrdinalIgnoreCase);

        // Parse declared capabilities from manifest
        var declaredNames = ParseDeclaredCapabilities(manifest.Capabilities);

        // Only return capabilities that are explicitly declared in the manifest
        foreach (var name in declaredNames)
        {
            if (_capabilityFactories.TryGetValue(name, out var factory))
            {
                grantedCapabilities[name] = factory(executionContext.PluginId);
            }
            // If declared but no factory registered, skip silently
            // (the capability implementation may not be available in this runtime instance)
        }

        return grantedCapabilities;
    }

    /// <summary>
    /// Validates that a plugin has access to a specific capability.
    /// Enforces deny-by-default: if the capability is not in the resolved set,
    /// logs the denial as a security event and throws CapabilityDeniedException.
    /// </summary>
    /// <param name="capabilityName">The capability being accessed.</param>
    /// <param name="resolvedCapabilities">The set of capabilities resolved for this plugin.</param>
    /// <param name="executionContext">The current execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved capability instance.</returns>
    /// <exception cref="CapabilityDeniedException">
    /// Thrown when the requested capability is not declared in the manifest.
    /// </exception>
    public async Task<ICapability> ValidateAccessAsync(
        string capabilityName,
        IReadOnlyDictionary<string, ICapability> resolvedCapabilities,
        ExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capabilityName);
        ArgumentNullException.ThrowIfNull(resolvedCapabilities);
        ArgumentNullException.ThrowIfNull(executionContext);

        if (resolvedCapabilities.TryGetValue(capabilityName, out var capability))
        {
            return capability;
        }

        // Deny-by-default: undeclared capability results in immediate denial
        await LogCapabilityDenialAsync(capabilityName, executionContext, cancellationToken);

        throw new CapabilityDeniedException(capabilityName, executionContext.PluginId);
    }

    /// <summary>
    /// Logs a capability denial as a security event in audit_logs.
    /// Records the denial with full context for security auditing.
    /// </summary>
    private async Task LogCapabilityDenialAsync(
        string capabilityName,
        ExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        var entry = new AuditEntry(
            TraceId: executionContext.ExecutionId,
            ActorId: executionContext.UserId ?? "system",
            ActorType: "System",
            Action: "capability_denied",
            ResourceType: "Capability",
            ResourceId: capabilityName,
            Result: "Denied",
            IpAddress: null,
            Metadata: new Dictionary<string, object>
            {
                ["pluginId"] = executionContext.PluginId.ToString(),
                ["capabilityName"] = capabilityName,
                ["reason"] = $"Capability '{capabilityName}' not declared in manifest",
                ["executionId"] = executionContext.ExecutionId,
                ["category"] = "Security"
            });

        await _auditLogger.LogAsync(entry, cancellationToken);
    }

    private static IReadOnlyList<string> ParseDeclaredCapabilities(JsonElement capabilities)
    {
        if (capabilities.ValueKind == JsonValueKind.Undefined || capabilities.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<string>();
        }

        if (capabilities.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var names = new List<string>();
        foreach (var element in capabilities.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var name = element.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }
            else if (element.ValueKind == JsonValueKind.Object &&
                     element.TryGetProperty("name", out var nameProp))
            {
                var name = nameProp.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }
}
