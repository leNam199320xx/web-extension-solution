using System.Net.Http.Json;
using PluginRuntime.ConsumerPortal.Models;
using PluginRuntime.ConsumerPortal.Models.DTOs;
using PluginRuntime.ConsumerPortal.Models.Requests;

namespace PluginRuntime.ConsumerPortal.Services;

public sealed class ApiKeyService : IApiKeyService
{
    private readonly HttpClient _http;
    public ApiKeyService(HttpClient http) => _http = http;

    public async Task<ApiResult<ApiKeyListDto>> GetKeysAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ApiKeyListDto>("api/keys", ct);
            return result is not null
                ? ApiResult<ApiKeyListDto>.Success(result)
                : ApiResult<ApiKeyListDto>.Success(new ApiKeyListDto([], 0));
        }
        catch (HttpRequestException) { return ApiResult<ApiKeyListDto>.NetworkFailure(); }
    }

    public async Task<ApiResult<ApiKeyGenerationResult>> GenerateKeyAsync(GenerateKeyRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/keys", request, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiKeyGenerationResult>(ct);
                return result is not null
                    ? ApiResult<ApiKeyGenerationResult>.Success(result)
                    : ApiResult<ApiKeyGenerationResult>.NetworkFailure();
            }
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct);
            return ApiResult<ApiKeyGenerationResult>.Fail(error?.Error ?? new ApiError("UNKNOWN", "Key generation failed", null));
        }
        catch (HttpRequestException) { return ApiResult<ApiKeyGenerationResult>.NetworkFailure(); }
    }

    public async Task<ApiResult<ApiKeyGenerationResult>> RotateKeyAsync(Guid keyId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync($"api/keys/{keyId}/rotate", null, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiKeyGenerationResult>(ct);
                return result is not null
                    ? ApiResult<ApiKeyGenerationResult>.Success(result)
                    : ApiResult<ApiKeyGenerationResult>.NetworkFailure();
            }
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct);
            return ApiResult<ApiKeyGenerationResult>.Fail(error?.Error ?? new ApiError("UNKNOWN", "Key rotation failed", null));
        }
        catch (HttpRequestException) { return ApiResult<ApiKeyGenerationResult>.NetworkFailure(); }
    }

    public async Task<ApiResult<bool>> RevokeKeyAsync(Guid keyId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/keys/{keyId}", ct);
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Success(true)
                : ApiResult<bool>.Fail(new ApiError("REVOKE_FAILED", "Failed to revoke key", null));
        }
        catch (HttpRequestException) { return ApiResult<bool>.NetworkFailure(); }
    }
}
