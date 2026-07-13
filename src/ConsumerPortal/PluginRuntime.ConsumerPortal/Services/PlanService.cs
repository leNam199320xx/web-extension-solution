using System.Net.Http.Json;
using PluginRuntime.ConsumerPortal.Models;
using PluginRuntime.ConsumerPortal.Models.DTOs;
using PluginRuntime.ConsumerPortal.Models.Requests;

namespace PluginRuntime.ConsumerPortal.Services;

public sealed class PlanService : IPlanService
{
    private readonly HttpClient _http;
    public PlanService(HttpClient http) => _http = http;

    public async Task<ApiResult<List<PlanDto>>> GetAllPlansAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<PlanDto>>("api/plans", ct);
            return ApiResult<List<PlanDto>>.Success(result ?? []);
        }
        catch (HttpRequestException) { return ApiResult<List<PlanDto>>.NetworkFailure(); }
    }

    public async Task<ApiResult<PlanChangeResult>> ChangePlanAsync(PlanChangeRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/plans/change", request, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlanChangeResult>(ct);
                return result is not null
                    ? ApiResult<PlanChangeResult>.Success(result)
                    : ApiResult<PlanChangeResult>.NetworkFailure();
            }
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct);
            return ApiResult<PlanChangeResult>.Fail(error?.Error ?? new ApiError("UNKNOWN", "Plan change failed", null));
        }
        catch (HttpRequestException) { return ApiResult<PlanChangeResult>.NetworkFailure(); }
    }

    public async Task<ApiResult<string>> GetStripeCheckoutUrlAsync(Guid planId, CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetStringAsync($"api/plans/{planId}/checkout", ct);
            return ApiResult<string>.Success(result);
        }
        catch (HttpRequestException) { return ApiResult<string>.NetworkFailure(); }
    }
}
