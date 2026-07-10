using System.Net.Http.Json;
using PluginRuntime.ConsumerPortal.Models;
using PluginRuntime.ConsumerPortal.Models.DTOs;
using PluginRuntime.ConsumerPortal.Models.Requests;

namespace PluginRuntime.ConsumerPortal.Services;

public sealed class TenantService : ITenantService
{
    private readonly HttpClient _http;
    public TenantService(HttpClient http) => _http = http;

    public async Task<ApiResult<TenantDto>> GetCurrentTenantAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<TenantDto>("api/tenants/me", ct);
            return result is not null ? ApiResult<TenantDto>.Success(result) : ApiResult<TenantDto>.Fail(new ApiError("NOT_FOUND", "Tenant not found", null));
        }
        catch (HttpRequestException) { return ApiResult<TenantDto>.NetworkFailure(); }
    }

    public async Task<ApiResult<TenantRegistrationResult>> RegisterAsync(TenantRegistrationRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/tenants", request, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TenantRegistrationResult>(ct);
                return result is not null ? ApiResult<TenantRegistrationResult>.Success(result) : ApiResult<TenantRegistrationResult>.NetworkFailure();
            }
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct);
            return ApiResult<TenantRegistrationResult>.Fail(error?.Error ?? new ApiError("UNKNOWN", "Registration failed", null));
        }
        catch (HttpRequestException) { return ApiResult<TenantRegistrationResult>.NetworkFailure(); }
    }

    public async Task<ApiResult<bool>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PutAsJsonAsync("api/tenants/me/profile", request, ct);
            return response.IsSuccessStatusCode ? ApiResult<bool>.Success(true) : ApiResult<bool>.Fail(new ApiError("UPDATE_FAILED", "Profile update failed", null));
        }
        catch (HttpRequestException) { return ApiResult<bool>.NetworkFailure(); }
    }

    public async Task<ApiResult<bool>> UpdateNotificationPreferencesAsync(NotificationPreferencesRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PutAsJsonAsync("api/tenants/me/notifications", request, ct);
            return response.IsSuccessStatusCode ? ApiResult<bool>.Success(true) : ApiResult<bool>.Fail(new ApiError("UPDATE_FAILED", "Preferences update failed", null));
        }
        catch (HttpRequestException) { return ApiResult<bool>.NetworkFailure(); }
    }
}
