using PluginRuntime.Marketplace.Models;

namespace PluginRuntime.Marketplace.Services;

public interface IProfileService
{
    Task<PublisherProfileDto?> GetPublisherProfileAsync(Guid publisherId, CancellationToken ct = default);
    Task<UserProfileDto?> GetCurrentUserProfileAsync(CancellationToken ct = default);
    Task<bool> UpdateProfileAsync(UpdateProfileDto profile, CancellationToken ct = default);
    Task<ApiKeyDto?> GenerateApiKeyAsync(string name, CancellationToken ct = default);
    Task<bool> RevokeApiKeyAsync(Guid keyId, CancellationToken ct = default);
    Task<ApiKeyListDto?> GetApiKeysAsync(CancellationToken ct = default);
}
