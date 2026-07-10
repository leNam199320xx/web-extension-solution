using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using PluginRuntime.Api.Shared.Interfaces;
using PluginRuntime.Sdk;

namespace PluginRuntime.Api.Shared.Infrastructure;

/// <summary>
/// Discovers and loads system plugins from the plugins directory at startup.
/// System plugins have skip_signature_verification=true in their manifest.
/// They are loaded via AssemblyLoadContext but trusted (no signature check).
/// Routes declared in manifest are registered as endpoints.
/// </summary>
public sealed class SystemPluginLoader
{
    private readonly ILogger<SystemPluginLoader> _logger;
    private readonly Dictionary<string, LoadedSystemPlugin> _plugins = new();

    public SystemPluginLoader(ILogger<SystemPluginLoader> logger)
    {
        _logger = logger;
    }

    public IReadOnlyDictionary<string, LoadedSystemPlugin> Plugins => _plugins;

    /// <summary>
    /// Scans the plugins directory for system plugins and loads them.
    /// </summary>
    public void LoadFromDirectory(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.LogWarning("Plugins directory not found: {Dir}", pluginsDirectory);
            return;
        }

        foreach (var dir in Directory.GetDirectories(pluginsDirectory))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var manifestJson = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<SystemPluginManifest>(manifestJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest is null) continue;
                if (!manifest.IsSystem && !manifest.SkipSignatureVerification) continue;

                // Look for DLL: first in same dir, then in bin/Debug/net10.0/
                var dllPath = Path.Combine(dir, manifest.EntryPoint);
                if (!File.Exists(dllPath))
                    dllPath = Path.Combine(dir, "bin", "Debug", "net10.0", manifest.EntryPoint);

                if (!File.Exists(dllPath))
                {
                    _logger.LogWarning("System plugin DLL not found: {DllPath} (tried bin/Debug/net10.0/ too)", manifest.EntryPoint);
                    continue;
                }

                var alc = new AssemblyLoadContext(manifest.ExtensionId, isCollectible: false);
                var assembly = alc.LoadFromAssemblyPath(Path.GetFullPath(dllPath));
                var pluginType = assembly.GetType(manifest.EntryClass!);

                if (pluginType is null || !typeof(IPlugin).IsAssignableFrom(pluginType))
                {
                    _logger.LogWarning("System plugin {Id} entry class {Class} not found or does not implement IPlugin",
                        manifest.ExtensionId, manifest.EntryClass);
                    continue;
                }

                var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;

                _plugins[manifest.ExtensionId] = new LoadedSystemPlugin(manifest, plugin, assembly);

                _logger.LogInformation("Loaded system plugin: {Id} v{Version} ({Name})",
                    manifest.ExtensionId, manifest.Version, manifest.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load system plugin from {Dir}", dir);
            }
        }

        _logger.LogInformation("System plugin loading complete. {Count} plugin(s) loaded.", _plugins.Count);
    }
}

public sealed record LoadedSystemPlugin(SystemPluginManifest Manifest, IPlugin Instance, Assembly Assembly);

public sealed record SystemPluginManifest
{
    [System.Text.Json.Serialization.JsonPropertyName("extension_id")]
    public string ExtensionId { get; init; } = "";
    public string Version { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("entry_point")]
    public string EntryPoint { get; init; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("entry_class")]
    public string? EntryClass { get; init; }
    public string Type { get; init; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("is_system")]
    public bool IsSystem { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("skip_signature_verification")]
    public bool SkipSignatureVerification { get; init; }
    public List<RouteDefinition> Routes { get; init; } = [];
}

public sealed record RouteDefinition
{
    public string Method { get; init; } = "";
    public string Path { get; init; } = "";
    public string Action { get; init; } = "";
}
