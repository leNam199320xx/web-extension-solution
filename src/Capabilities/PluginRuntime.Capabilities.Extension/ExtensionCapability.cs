using System.Text.Json;
using PluginRuntime.Capabilities.Abstractions;
using PluginRuntime.Core.Enums;
using PluginRuntime.Core.Exceptions;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Extension;

/// <summary>
/// Provides controlled inter-extension invocation with permission, existence,
/// and visibility checks enforced in strict priority order.
/// </summary>
public class ExtensionCapability : IExtensionCapability
{
    private readonly Guid _callerId;
    private readonly IReadOnlySet<string> _callerPermissions;
    private readonly Guid _callerOwnerId;
    private readonly IExtensionRegistryRepository _extensionRegistry;
    private readonly IExtensionSubscriptionRepository _subscriptionRepository;
    private readonly IPluginRepository _pluginRepository;
    private readonly IExecutionPipeline _executionPipeline;

    public string Name => "extension";
    public string Version => "1.0";

    public ExtensionCapability(
        Guid callerId,
        IReadOnlySet<string> callerPermissions,
        Guid callerOwnerId,
        IExtensionRegistryRepository extensionRegistry,
        IExtensionSubscriptionRepository subscriptionRepository,
        IPluginRepository pluginRepository,
        IExecutionPipeline executionPipeline)
    {
        _callerId = callerId;
        _callerPermissions = callerPermissions ?? throw new ArgumentNullException(nameof(callerPermissions));
        _callerOwnerId = callerOwnerId;
        _extensionRegistry = extensionRegistry ?? throw new ArgumentNullException(nameof(extensionRegistry));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _pluginRepository = pluginRepository ?? throw new ArgumentNullException(nameof(pluginRepository));
        _executionPipeline = executionPipeline ?? throw new ArgumentNullException(nameof(executionPipeline));
    }

    /// <inheritdoc/>
    public async Task<ExtensionInvocationResult> InvokeAsync(
        string targetExtensionId,
        object? input = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetExtensionId);

        // Priority order: permission > existence > visibility
        // Check (1): Caller must have extension:invoke:{targetId} permission
        VerifyPermission(targetExtensionId);

        // Check (2): Target must exist and be Active
        var extensionRecord = await VerifyExistenceAsync(targetExtensionId, cancellationToken);

        // Check (3): Visibility rules
        await VerifyVisibilityAsync(targetExtensionId, extensionRecord, cancellationToken);

        // All checks passed — execute the target extension via pipeline
        var startTime = DateTime.UtcNow;

        var inputElement = input is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(input);

        var request = new Core.ValueObjects.ExecutionRequest(
            PluginId: extensionRecord.PluginId,
            Version: null,
            Input: inputElement,
            CorrelationId: null,
            UserId: null,
            TenantId: null);

        var result = await _executionPipeline.ProcessAsync(request, cancellationToken);
        var duration = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

        return new ExtensionInvocationResult
        {
            Success = result.Success,
            Data = result.Data,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            TargetExecutionId = result.ExecutionId ?? "",
            DurationMs = duration
        };
    }

    /// <inheritdoc/>
    public async Task<bool> CanInvokeAsync(
        string targetExtensionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetExtensionId))
            return false;

        // Check permission
        var requiredPermission = $"extension:invoke:{targetExtensionId}";
        if (!_callerPermissions.Contains(requiredPermission))
            return false;

        // Check existence and Active status
        var extensionRecord = await _extensionRegistry.GetByIdAsync(targetExtensionId, cancellationToken);
        if (extensionRecord is null)
            return false;

        var plugin = await _pluginRepository.GetByIdAsync(extensionRecord.PluginId, cancellationToken);
        if (plugin is null || plugin.Status != PluginStatus.Active)
            return false;

        // Check visibility
        return await CheckVisibilityAsync(targetExtensionId, extensionRecord, cancellationToken);
    }

    /// <summary>
    /// Verifies that the caller's manifest declares extension:invoke:{targetId} permission.
    /// Throws CapabilityDeniedException if not declared (highest priority check).
    /// </summary>
    private void VerifyPermission(string targetExtensionId)
    {
        var requiredPermission = $"extension:invoke:{targetExtensionId}";
        if (!_callerPermissions.Contains(requiredPermission))
        {
            throw new CapabilityDeniedException(requiredPermission, _callerId);
        }
    }

    /// <summary>
    /// Verifies target extension exists in registry and associated plugin is Active.
    /// Throws ExtensionNotFoundException if not found or not active (second priority check).
    /// </summary>
    private async Task<ExtensionRegistryRecord> VerifyExistenceAsync(
        string targetExtensionId,
        CancellationToken cancellationToken)
    {
        var extensionRecord = await _extensionRegistry.GetByIdAsync(targetExtensionId, cancellationToken);
        if (extensionRecord is null)
        {
            throw new ExtensionNotFoundException(targetExtensionId);
        }

        var plugin = await _pluginRepository.GetByIdAsync(extensionRecord.PluginId, cancellationToken);
        if (plugin is null || plugin.Status != PluginStatus.Active)
        {
            throw new ExtensionNotFoundException(targetExtensionId);
        }

        return extensionRecord;
    }

    /// <summary>
    /// Verifies visibility rules: Public allows all, Private allows same owner only,
    /// Subscription requires an active approved subscription.
    /// Throws AccessDeniedException if access denied (lowest priority check).
    /// </summary>
    private async Task VerifyVisibilityAsync(
        string targetExtensionId,
        ExtensionRegistryRecord extensionRecord,
        CancellationToken cancellationToken)
    {
        if (!await CheckVisibilityAsync(targetExtensionId, extensionRecord, cancellationToken))
        {
            throw new AccessDeniedException(targetExtensionId, _callerId);
        }
    }

    /// <summary>
    /// Returns true if visibility rules allow access, false otherwise.
    /// </summary>
    private async Task<bool> CheckVisibilityAsync(
        string targetExtensionId,
        ExtensionRegistryRecord extensionRecord,
        CancellationToken cancellationToken)
    {
        switch (extensionRecord.Visibility)
        {
            case Visibility.Public:
                return true;

            case Visibility.Private:
                // Same owner only
                return extensionRecord.AuthorId == _callerOwnerId;

            case Visibility.Subscription:
                // Must have an active approved subscription
                return await HasActiveSubscriptionAsync(targetExtensionId, cancellationToken);

            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the caller has an active, approved subscription to the target extension
    /// that has not expired.
    /// </summary>
    private async Task<bool> HasActiveSubscriptionAsync(
        string targetExtensionId,
        CancellationToken cancellationToken)
    {
        // Look up the caller's extension ID from the registry
        var callerExtension = await _extensionRegistry.GetByPluginIdAsync(_callerId, cancellationToken);
        if (callerExtension is null)
            return false;

        var subscription = await _subscriptionRepository.GetBySourceAndTargetAsync(
            callerExtension.ExtensionId,
            targetExtensionId,
            cancellationToken);

        if (subscription is null)
            return false;

        // Must be Approved status
        if (subscription.Status != SubscriptionStatus.Approved)
            return false;

        // Must not be expired (expires_at > now, or null means no expiry)
        if (subscription.ExpiresAt.HasValue && subscription.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return true;
    }
}
