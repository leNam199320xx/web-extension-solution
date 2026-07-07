using PluginRuntime.Core.Enums;

namespace PluginRuntime.Core.Interfaces;

public record ExtensionRegistryRecord(
    string ExtensionId,
    Guid PluginId,
    string DisplayName,
    string? Description,
    Guid AuthorId,
    Visibility Visibility,
    string? Category,
    string? LatestVersion,
    int TotalVersions,
    int SubscriberCount,
    string? InvocationPolicy,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public interface IExtensionRegistryRepository
{
    Task<ExtensionRegistryRecord?> GetByIdAsync(string extensionId, CancellationToken cancellationToken);
    Task<ExtensionRegistryRecord?> GetByPluginIdAsync(Guid pluginId, CancellationToken cancellationToken);
    Task AddAsync(ExtensionRegistryRecord extension, CancellationToken cancellationToken);
    Task UpdateAsync(ExtensionRegistryRecord extension, CancellationToken cancellationToken);
}
