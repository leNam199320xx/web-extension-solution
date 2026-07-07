using PluginRuntime.Core.Enums;

namespace PluginRuntime.Core.Entities;

public class PluginVersion
{
    public Guid VersionId { get; init; }
    public Guid PluginId { get; init; }
    public string Version { get; init; }
    public string StorageUri { get; init; }
    public string Sha256 { get; init; }
    public string EntryPoint { get; init; }
    public string EntryClass { get; init; }
    public PluginVersionStatus Status { get; init; }
    public Guid? ApprovedBy { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime CreatedAt { get; init; }

    public PluginVersion(
        Guid versionId,
        Guid pluginId,
        string version,
        string storageUri,
        string sha256,
        string entryPoint,
        string entryClass,
        PluginVersionStatus status = PluginVersionStatus.Draft,
        Guid? approvedBy = null,
        DateTime? approvedAt = null,
        DateTime? createdAt = null)
    {
        if (versionId == Guid.Empty)
            throw new ArgumentException("VersionId must not be empty.", nameof(versionId));
        if (pluginId == Guid.Empty)
            throw new ArgumentException("PluginId must not be empty.", nameof(pluginId));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version must not be null or empty.", nameof(version));
        if (string.IsNullOrWhiteSpace(storageUri))
            throw new ArgumentException("StorageUri must not be null or empty.", nameof(storageUri));
        if (string.IsNullOrWhiteSpace(sha256))
            throw new ArgumentException("Sha256 must not be null or empty.", nameof(sha256));
        if (string.IsNullOrWhiteSpace(entryPoint))
            throw new ArgumentException("EntryPoint must not be null or empty.", nameof(entryPoint));
        if (string.IsNullOrWhiteSpace(entryClass))
            throw new ArgumentException("EntryClass must not be null or empty.", nameof(entryClass));

        VersionId = versionId;
        PluginId = pluginId;
        Version = version;
        StorageUri = storageUri;
        Sha256 = sha256;
        EntryPoint = entryPoint;
        EntryClass = entryClass;
        Status = status;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
