namespace PluginRuntime.Core.Exceptions;

/// <summary>
/// Thrown when a plugin attempts to use a capability not declared in its manifest.
/// Deny-by-default: undeclared capabilities result in immediate denial.
/// </summary>
public class CapabilityDeniedException : PluginRuntimeException
{
    public string CapabilityName { get; }
    public Guid PluginId { get; }

    public CapabilityDeniedException(string capabilityName, Guid pluginId)
        : base(
            "CAPABILITY_DENIED",
            "Security",
            $"Capability '{capabilityName}' not declared in manifest for plugin '{pluginId}'.")
    {
        CapabilityName = capabilityName;
        PluginId = pluginId;
    }
}
