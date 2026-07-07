namespace PluginRuntime.Core.Interfaces;

public record RevocationRecord(
    Guid RevocationId,
    Guid VersionId,
    string Reason,
    Guid RevokedBy,
    DateTime RevokedAt,
    DateTime? ExpiresAt);

public interface IRevocationRepository
{
    Task<RevocationRecord?> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken);
}
