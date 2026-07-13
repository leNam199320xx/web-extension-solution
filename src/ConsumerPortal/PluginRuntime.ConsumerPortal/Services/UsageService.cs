using System.Net.Http.Json;
using PluginRuntime.ConsumerPortal.Models;
using PluginRuntime.ConsumerPortal.Models.DTOs;

namespace PluginRuntime.ConsumerPortal.Services;

public sealed class UsageService : IUsageService
{
    private readonly HttpClient _http;
    public UsageService(HttpClient http) => _http = http;

    public async Task<ApiResult<DashboardUsageDto>> GetDashboardUsageAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<DashboardUsageDto>("api/usage/dashboard", ct);
            return result is not null
                ? ApiResult<DashboardUsageDto>.Success(result)
                : ApiResult<DashboardUsageDto>.Fail(new ApiError("NOT_FOUND", "Dashboard data not found", null));
        }
        catch (HttpRequestException) { return ApiResult<DashboardUsageDto>.NetworkFailure(); }
    }

    public async Task<ApiResult<List<UsageAggregateDto>>> GetDailyUsageAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<UsageAggregateDto>>(
                $"api/usage/daily?from={startDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}", ct);
            return ApiResult<List<UsageAggregateDto>>.Success(result ?? []);
        }
        catch (HttpRequestException) { return ApiResult<List<UsageAggregateDto>>.NetworkFailure(); }
    }

    public async Task<ApiResult<UsageSummaryDto>> GetUsageSummaryAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<UsageSummaryDto>(
                $"api/usage/summary?from={startDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}", ct);
            return result is not null
                ? ApiResult<UsageSummaryDto>.Success(result)
                : ApiResult<UsageSummaryDto>.Fail(new ApiError("NOT_FOUND", "Summary not found", null));
        }
        catch (HttpRequestException) { return ApiResult<UsageSummaryDto>.NetworkFailure(); }
    }
}
