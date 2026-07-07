using System.Collections.Concurrent;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Sdk;

namespace PluginRuntime.Runtime.Loading;

/// <summary>
/// Manages plugin loading into isolated AssemblyLoadContexts.
/// Each plugin version gets its own collectible ALC ensuring no shared mutable state.
/// Tracks loaded ALCs for unload/hot-reload support.
/// </summary>
public sealed class PluginLoader : IPluginLoader
{
    private readonly ConcurrentDictionary<string, PluginLoadEntry> _loadedPlugins = new();
    private readonly IPluginBinaryStore _binaryStore;

    public PluginLoader(IPluginBinaryStore binaryStore)
    {
        _binaryStore = binaryStore ?? throw new ArgumentNullException(nameof(binaryStore));
    }

    public async Task<IPlugin> LoadAsync(PluginVersion version, Manifest manifest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(manifest);
        cancellationToken.ThrowIfCancellationRequested();

        var key = GetKey(version.PluginId.ToString(), version.Version);

        // Warm path: return already-loaded plugin instance
        if (_loadedPlugins.TryGetValue(key, out var existing))
        {
            return existing.Plugin;
        }

        // Get DLL bytes from storage
        var dllBytes = await _binaryStore.GetPluginBinaryAsync(
            version.PluginId, version.VersionId, cancellationToken);

        if (dllBytes is null || dllBytes.Length == 0)
        {
            throw new InvalidOperationException(
                $"Plugin binary not found for plugin '{version.PluginId}' version '{version.VersionId}'.");
        }

        // Write DLL to temp path for ALC dependency resolution
        var tempDir = Path.Combine(
            Path.GetTempPath(), "plugin-runtime", version.PluginId.ToString(), version.Version);
        Directory.CreateDirectory(tempDir);
        var dllPath = Path.Combine(tempDir, version.EntryPoint);
        await File.WriteAllBytesAsync(dllPath, dllBytes, cancellationToken);

        // Create isolated ALC and load the plugin
        PluginAssemblyLoadContext? alc = null;
        try
        {
            alc = new PluginAssemblyLoadContext(dllPath);
            var assembly = alc.LoadFromAssemblyPath(dllPath);

            // Resolve entry point class
            var entryType = assembly.GetType(version.EntryClass);
            if (entryType is null)
            {
                alc.Unload();
                throw new InvalidOperationException(
                    $"Entry class '{version.EntryClass}' not found in assembly '{version.EntryPoint}'.");
            }

            if (!typeof(IPlugin).IsAssignableFrom(entryType))
            {
                alc.Unload();
                throw new InvalidOperationException(
                    $"Entry class '{version.EntryClass}' does not implement IPlugin.");
            }

            var plugin = (IPlugin)(Activator.CreateInstance(entryType)
                ?? throw new InvalidOperationException(
                    $"Failed to instantiate entry class '{version.EntryClass}'."));

            var entry = new PluginLoadEntry(alc, plugin, dllPath);
            _loadedPlugins[key] = entry;

            return plugin;
        }
        catch
        {
            // On any failure: unload ALC to leave no residual state
            alc?.Unload();
            throw;
        }
    }

    public Task UnloadAsync(string pluginId, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var key = GetKey(pluginId, version);

        if (_loadedPlugins.TryRemove(key, out var entry))
        {
            entry.LoadContext.Unload();

            // Best-effort cleanup of temp files
            var dir = Path.GetDirectoryName(entry.DllPath);
            if (dir is not null && Directory.Exists(dir))
            {
                try { Directory.Delete(dir, recursive: true); }
                catch { /* best-effort cleanup */ }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether a plugin version is currently loaded.
    /// Used by HotReloadManager to determine if drain is needed.
    /// </summary>
    public bool IsLoaded(string pluginId, string version) =>
        _loadedPlugins.ContainsKey(GetKey(pluginId, version));

    /// <summary>
    /// Gets the count of currently loaded plugins. Used for monitoring/diagnostics.
    /// </summary>
    public int LoadedCount => _loadedPlugins.Count;

    private static string GetKey(string pluginId, string version) => $"{pluginId}:{version}";

    private sealed record PluginLoadEntry(
        PluginAssemblyLoadContext LoadContext,
        IPlugin Plugin,
        string DllPath);
}
