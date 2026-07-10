using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Services;

public interface IUploadService
{
    Task<ManifestPreviewDto?> ParseManifestAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    Task<UploadResultDto?> UploadPluginAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}
