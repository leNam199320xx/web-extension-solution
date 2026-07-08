namespace PluginRuntime.Api.Shared.DTOs;

/// <summary>
/// Pagination parameters for list queries.
/// </summary>
public sealed record PaginationParams
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public int Page { get; init; } = DefaultPage;
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// Returns a normalized instance with Page >= 1 and PageSize clamped to [1, MaxPageSize].
    /// </summary>
    public PaginationParams Normalize() => this with
    {
        Page = Math.Max(1, Page),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize)
    };

    public int Skip => (Math.Max(1, Page) - 1) * Math.Clamp(PageSize, 1, MaxPageSize);
    public int Take => Math.Clamp(PageSize, 1, MaxPageSize);
}
