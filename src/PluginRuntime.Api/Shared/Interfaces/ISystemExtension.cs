namespace PluginRuntime.Api.Shared.Interfaces;

/// <summary>
/// Marker interface for system extensions.
/// System extensions are pre-installed plugins that:
/// - Have their own HTTP routes (unlike regular plugins that only run via /api/plugins/execute)
/// - Cannot be uninstalled or disabled
/// - Are visible in the plugin registry with type "system"
/// - Follow the same manifest format as regular extensions
/// </summary>
public interface ISystemExtension
{
    /// <summary>Unique extension ID (e.g. "system.auth")</summary>
    string ExtensionId { get; }

    /// <summary>Display name</summary>
    string Name { get; }

    /// <summary>Semantic version</summary>
    string Version { get; }

    /// <summary>Extension description</summary>
    string Description { get; }
}
