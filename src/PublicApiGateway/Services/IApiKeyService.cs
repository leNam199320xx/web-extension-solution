using PublicApiGateway.Models;

namespace PublicApiGateway.Services;

public interface IApiKeyService
{
    Task<ApiKeyInfo?> ValidateAsync(string apiKey, CancellationToken ct);
    Task InvalidateCacheAsync(string apiKey, CancellationToken ct);
}
