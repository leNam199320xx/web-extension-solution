using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Search;

public interface ISearchEngine
{
    SearchResult<ExtensionSummaryDto> Search(IEnumerable<ExtensionSummaryDto> items, SearchCriteria criteria, int page = 1, int pageSize = 20);
}
