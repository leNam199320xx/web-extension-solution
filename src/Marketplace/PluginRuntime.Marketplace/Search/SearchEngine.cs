using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Search;

/// <summary>
/// Client-side search engine with text search, exact-match filtering, and pagination.
/// </summary>
public sealed class SearchEngine : ISearchEngine
{
    private const int DefaultPageSize = 20;

    public SearchResult<ExtensionSummaryDto> Search(
        IEnumerable<ExtensionSummaryDto> items,
        SearchCriteria criteria,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        var filtered = items.AsEnumerable();

        // Text search: case-insensitive contains on Name, ShortDescription, ExtensionId
        if (!string.IsNullOrWhiteSpace(criteria.Text))
        {
            var text = criteria.Text.Trim();
            filtered = filtered.Where(e =>
                e.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                e.ShortDescription.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                e.ExtensionId.ToString().Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        // Category exact match
        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            filtered = filtered.Where(e =>
                string.Equals(e.Category, criteria.Category, StringComparison.OrdinalIgnoreCase));
        }

        // Risk level exact match
        if (!string.IsNullOrWhiteSpace(criteria.RiskLevel))
        {
            filtered = filtered.Where(e =>
                string.Equals(e.RiskLevel, criteria.RiskLevel, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = filtered.Count();
        var pagedItems = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new SearchResult<ExtensionSummaryDto>(pagedItems, totalCount, page, pageSize);
    }
}
