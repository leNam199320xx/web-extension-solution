using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Abstractions;

public interface IExtensionCapability : ICapability
{
    /// <summary>
    /// Invoke another extension by ID.
    /// Requires "extension:invoke:{targetId}" permission in manifest.
    /// Target must be Public, or caller must have active Subscription.
    /// </summary>
    Task<ExtensionInvocationResult> InvokeAsync(
        string targetExtensionId,
        object? input = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a target extension is available and accessible.
    /// Returns false if target is not active, not visible, or no subscription.
    /// </summary>
    Task<bool> CanInvokeAsync(
        string targetExtensionId,
        CancellationToken cancellationToken = default);
}
