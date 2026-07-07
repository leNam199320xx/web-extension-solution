using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Security.Revocation;

public class RevocationChecker : IRevocationChecker
{
    private readonly ICacheService _cache;
    private readonly IRevocationRepository _revocationRepository;
    private readonly TimeSpan _cacheTtl;
    private const string CacheKeyPrefix = "revocation:";

    public RevocationChecker(
        ICacheService cache,
        IRevocationRepository revocationRepository,
        TimeSpan? cacheTtl = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _revocationRepository = revocationRepository ?? throw new ArgumentNullException(nameof(revocationRepository));
        _cacheTtl = cacheTtl ?? TimeSpan.FromSeconds(300);
    }

    public async Task<bool> IsRevokedAsync(Guid versionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = $"{CacheKeyPrefix}{versionId}";

        // Check cache first
        var cached = await _cache.GetAsync<RevocationCacheEntry>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return IsActiveRevocation(cached.IsRevoked, cached.ExpiresAt);
        }

        // Cache miss - query repository
        var record = await _revocationRepository.GetByVersionIdAsync(versionId, cancellationToken);

        if (record is null)
        {
            // Cache negative result (not revoked)
            await _cache.SetAsync(cacheKey, new RevocationCacheEntry(false, null), _cacheTtl, cancellationToken);
            return false;
        }

        // Cache positive result
        var entry = new RevocationCacheEntry(true, record.ExpiresAt);
        await _cache.SetAsync(cacheKey, entry, _cacheTtl, cancellationToken);

        return IsActiveRevocation(entry.IsRevoked, record.ExpiresAt);
    }

    private static bool IsActiveRevocation(bool isRevoked, DateTime? expiresAt)
    {
        if (!isRevoked)
            return false;

        // If no expiration, revocation is permanent (active)
        if (expiresAt is null)
            return true;

        // Expired revocations do NOT block execution
        return expiresAt.Value > DateTime.UtcNow;
    }
}

internal record RevocationCacheEntry(bool IsRevoked, DateTime? ExpiresAt);
