using System.Net.Http.Json;
using PluginRuntime.ConsumerPortal.Models;
using PluginRuntime.ConsumerPortal.Models.DTOs;
using PluginRuntime.ConsumerPortal.Models.Requests;

namespace PluginRuntime.ConsumerPortal.Services;

public sealed class SupportService : ISupportService
{
    private readonly HttpClient _http;
    public SupportService(HttpClient http) => _http = http;

    public async Task<ApiResult<SupportTicketResult>> SubmitTicketAsync(SupportTicketRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/support/tickets", request, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SupportTicketResult>(ct);
                return result is not null
                    ? ApiResult<SupportTicketResult>.Success(result)
                    : ApiResult<SupportTicketResult>.NetworkFailure();
            }
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct);
            return ApiResult<SupportTicketResult>.Fail(error?.Error ?? new ApiError("UNKNOWN", "Ticket submission failed", null));
        }
        catch (HttpRequestException) { return ApiResult<SupportTicketResult>.NetworkFailure(); }
    }
}
