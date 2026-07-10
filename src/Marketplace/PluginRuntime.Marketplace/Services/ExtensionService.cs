using System.Net.Http.Json;
using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Services;

public sealed class ExtensionService : IExtensionService
{
    private readonly HttpClient _http;

    public ExtensionService(HttpClient http) => _http = http;

    public async Task<PaginatedResult<ExtensionSummaryDto>> GetExtensionsAsync(ExtensionQuery query, CancellationToken ct = default)
    {
        var url = $"api/extensions?page={query.Page}&pageSize={query.PageSize}";
        if (!string.IsNullOrWhiteSpace(query.SearchText)) url += $"&search={Uri.EscapeDataString(query.SearchText)}";
        if (!string.IsNullOrWhiteSpace(query.Category)) url += $"&category={Uri.EscapeDataString(query.Category)}";
        if (!string.IsNullOrWhiteSpace(query.RiskLevel)) url += $"&riskLevel={Uri.EscapeDataString(query.RiskLevel)}";

        return await _http.GetFromJsonAsync<PaginatedResult<ExtensionSummaryDto>>(url, ct)
            ?? new PaginatedResult<ExtensionSummaryDto>([], 0, 1, 20);
    }

    public async Task<ExtensionDetailDto?> GetExtensionDetailAsync(Guid extensionId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<ExtensionDetailDto>($"api/extensions/{extensionId}", ct);

    public async Task<List<ExtensionSummaryDto>> GetFeaturedExtensionsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ExtensionSummaryDto>>("api/extensions/featured", ct) ?? [];

    public async Task<EcosystemStatsDto?> GetEcosystemStatsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<EcosystemStatsDto>("api/extensions/stats", ct);

    public async Task<List<ExtensionSummaryDto>> GetMyExtensionsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ExtensionSummaryDto>>("api/extensions/mine", ct) ?? [];

    public async Task<List<VersionHistoryDto>> GetVersionHistoryAsync(Guid extensionId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<VersionHistoryDto>>($"api/extensions/{extensionId}/versions", ct) ?? [];
}
