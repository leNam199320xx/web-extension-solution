using System.Net.Http.Json;
using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Services;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly HttpClient _http;

    public SubscriptionService(HttpClient http) => _http = http;

    public async Task<SubscriptionResponseDto?> RequestSubscriptionAsync(SubscriptionRequestDto request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/subscriptions", request, ct);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SubscriptionResponseDto>(ct)
            : null;
    }

    public async Task<List<SubscriptionDto>> GetOutgoingRequestsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SubscriptionDto>>("api/subscriptions/outgoing", ct) ?? [];

    public async Task<List<SubscriptionDto>> GetIncomingRequestsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SubscriptionDto>>("api/subscriptions/incoming", ct) ?? [];

    public async Task<SubscriptionResponseDto?> DecideSubscriptionAsync(SubscriptionDecisionDto decision, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/subscriptions/{decision.SubscriptionId}/decide", decision, ct);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SubscriptionResponseDto>(ct)
            : null;
    }
}
