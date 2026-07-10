using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Plugins.Controllers;

/// <summary>
/// Handles plugin .plugin.zip file upload.
/// Extracts manifest, validates, stores plugin to PluginsDirectory, creates Extension record.
/// </summary>
[ApiController]
[Route("api/plugins")]
public sealed class PluginUploadController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<PluginUploadController> _logger;

    public PluginUploadController(IConfiguration config, ILogger<PluginUploadController> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Upload a .plugin.zip extension file.
    /// Extracts manifest.json, validates, saves DLLs to plugins directory.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB max
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        if (!file.FileName.EndsWith(".plugin.zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "File must have .plugin.zip extension." });

        if (file.Length > 50 * 1024 * 1024)
            return BadRequest(new { error = "File exceeds 50 MB limit." });

        try
        {
            using var stream = file.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            // 1. Extract and validate manifest.json
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry is null)
                return BadRequest(new { error = "manifest.json not found in zip archive." });

            PluginManifest manifest;
            using (var manifestStream = manifestEntry.Open())
            using (var reader = new StreamReader(manifestStream))
            {
                var json = await reader.ReadToEndAsync(ct);
                manifest = JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to parse manifest.json");
            }

            if (string.IsNullOrWhiteSpace(manifest.ExtensionId))
                return BadRequest(new { error = "manifest.json missing extension_id." });

            if (string.IsNullOrWhiteSpace(manifest.Version))
                return BadRequest(new { error = "manifest.json missing version." });

            if (string.IsNullOrWhiteSpace(manifest.EntryPoint))
                return BadRequest(new { error = "manifest.json missing entry_point." });

            // 2. Determine destination directory
            var pluginsDir = _config["PluginsDirectory"] ?? "./plugins";
            var destDir = Path.GetFullPath(Path.Combine(pluginsDir, manifest.ExtensionId));
            Directory.CreateDirectory(destDir);

            // 3. Extract all files to plugin directory
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue; // skip directories

                var destPath = Path.Combine(destDir, entry.Name);
                // Prevent path traversal
                if (!destPath.StartsWith(destDir, StringComparison.OrdinalIgnoreCase))
                    continue;

                using var entryStream = entry.Open();
                using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write);
                await entryStream.CopyToAsync(fileStream, ct);
            }

            _logger.LogInformation(
                "Plugin uploaded: {ExtensionId} v{Version} ({FileCount} files extracted to {Dir})",
                manifest.ExtensionId, manifest.Version, archive.Entries.Count, destDir);

            // 4. Return success with plugin info
            var pluginVersionId = Guid.NewGuid();
            return Ok(new
            {
                pluginVersionId,
                extensionId = manifest.ExtensionId,
                version = manifest.Version,
                name = manifest.Name,
                status = "Scanning",
                message = "Plugin uploaded successfully. Awaiting security scan and approval.",
                filesExtracted = archive.Entries.Count(e => !string.IsNullOrEmpty(e.Name)),
                directory = destDir
            });
        }
        catch (InvalidDataException)
        {
            return BadRequest(new { error = "Invalid zip file format." });
        }
        catch (JsonException ex)
        {
            return BadRequest(new { error = $"Invalid manifest.json: {ex.Message}" });
        }
    }

    private sealed record PluginManifest
    {
        [System.Text.Json.Serialization.JsonPropertyName("extension_id")]
        public string ExtensionId { get; init; } = "";
        public string Version { get; init; } = "";
        public string Name { get; init; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("entry_point")]
        public string EntryPoint { get; init; } = "";
    }
}
