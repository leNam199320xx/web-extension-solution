using System.Net.Http.Json;
using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Services;

public sealed class ProfileService : IProfileService
{
    private readonly HttpClient _http;

    public ProfileService(HttpClient http) => _http = http;

    public async Task<PublisherProfileDto?> GetPublisherProfileAsync(Guid publisherId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<PublisherProfileDto>($"api/publishers/{publisherId}", ct);

    public async Task<UserProfileDto?> GetCurrentUserProfileAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<UserProfileDto>("api/profile", ct);

    public async Task<bool> UpdateProfileAsync(UpdateProfileDto profile, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/profile", profile, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<ApiKeyDto?> GenerateApiKeyAsync(string name, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/profile/keys", new { name }, ct);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ApiKeyDto>(ct)
            : null;
    }

    public async Task<bool> RevokeApiKeyAsync(Guid keyId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/profile/keys/{keyId}", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<ApiKeyListDto?> GetApiKeysAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<ApiKeyListDto>("api/profile/keys", ct);
}
