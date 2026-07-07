using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class ExtensionRegistryRepository : IExtensionRegistryRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public ExtensionRegistryRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExtensionRegistryRecord?> GetByIdAsync(string extensionId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ExtensionRegistry
            .FirstOrDefaultAsync(e => e.ExtensionId == extensionId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<ExtensionRegistryRecord?> GetByPluginIdAsync(Guid pluginId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ExtensionRegistry
            .FirstOrDefaultAsync(e => e.PluginId == pluginId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task AddAsync(ExtensionRegistryRecord extension, CancellationToken cancellationToken)
    {
        var entity = new ExtensionRegistryEntity
        {
            ExtensionId = extension.ExtensionId,
            PluginId = extension.PluginId,
            DisplayName = extension.DisplayName,
            Description = extension.Description,
            AuthorId = extension.AuthorId,
            Visibility = extension.Visibility,
            Category = extension.Category,
            LatestVersion = extension.LatestVersion,
            TotalVersions = extension.TotalVersions,
            SubscriberCount = extension.SubscriberCount,
            InvocationPolicy = extension.InvocationPolicy,
            CreatedAt = extension.CreatedAt,
            UpdatedAt = extension.UpdatedAt
        };

        await _dbContext.ExtensionRegistry.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExtensionRegistryRecord extension, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ExtensionRegistry
            .FirstOrDefaultAsync(e => e.ExtensionId == extension.ExtensionId, cancellationToken);

        if (entity is not null)
        {
            entity.PluginId = extension.PluginId;
            entity.DisplayName = extension.DisplayName;
            entity.Description = extension.Description;
            entity.AuthorId = extension.AuthorId;
            entity.Visibility = extension.Visibility;
            entity.Category = extension.Category;
            entity.LatestVersion = extension.LatestVersion;
            entity.TotalVersions = extension.TotalVersions;
            entity.SubscriberCount = extension.SubscriberCount;
            entity.InvocationPolicy = extension.InvocationPolicy;
            entity.UpdatedAt = extension.UpdatedAt;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static ExtensionRegistryRecord ToRecord(ExtensionRegistryEntity entity)
    {
        return new ExtensionRegistryRecord(
            entity.ExtensionId,
            entity.PluginId,
            entity.DisplayName,
            entity.Description,
            entity.AuthorId,
            entity.Visibility,
            entity.Category,
            entity.LatestVersion,
            entity.TotalVersions,
            entity.SubscriberCount,
            entity.InvocationPolicy,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
