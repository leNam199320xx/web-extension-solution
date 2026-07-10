using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Services;

public sealed class UploadService : IUploadService
{
    private readonly HttpClient _http;

    public UploadService(HttpClient http) => _http = http;

    public Task<ManifestPreviewDto?> ParseManifestAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        try
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry is null) return Task.FromResult<ManifestPreviewDto?>(null);

            using var reader = new StreamReader(manifestEntry.Open());
            var json = reader.ReadToEnd();
            var manifest = JsonSerializer.Deserialize<ManifestPreviewDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return Task.FromResult(manifest);
        }
        catch
        {
            return Task.FromResult<ManifestPreviewDto?>(null);
        }
    }

    public async Task<UploadResultDto?> UploadPluginAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        fileStream.Position = 0;
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);

        var response = await _http.PostAsync("api/plugins/upload", content, ct);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UploadResultDto>(ct)
            : null;
    }
}
