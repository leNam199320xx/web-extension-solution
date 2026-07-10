using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Services;

public interface IExtensionService
{
    Task<PaginatedResult<ExtensionSummaryDto>> GetExtensionsAsync(ExtensionQuery query, CancellationToken ct = default);
    Task<ExtensionDetailDto?> GetExtensionDetailAsync(Guid extensionId, CancellationToken ct = default);
    Task<List<ExtensionSummaryDto>> GetFeaturedExtensionsAsync(CancellationToken ct = default);
    Task<EcosystemStatsDto?> GetEcosystemStatsAsync(CancellationToken ct = default);
    Task<List<ExtensionSummaryDto>> GetMyExtensionsAsync(CancellationToken ct = default);
    Task<List<VersionHistoryDto>> GetVersionHistoryAsync(Guid extensionId, CancellationToken ct = default);
}
