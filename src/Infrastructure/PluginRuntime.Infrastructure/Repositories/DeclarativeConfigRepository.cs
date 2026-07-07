using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Repositories;

public class DeclarativeConfigRepository : IDeclarativeConfigRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public DeclarativeConfigRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DeclarativeConfigRecord?> GetByIdAsync(Guid configId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.DeclarativeConfigs
            .FirstOrDefaultAsync(c => c.ConfigId == configId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<DeclarativeConfigRecord>> GetByExtensionIdAsync(string extensionId, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.DeclarativeConfigs
            .Where(c => c.ExtensionId == extensionId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToList();
    }

    public async Task AddAsync(DeclarativeConfigRecord config, CancellationToken cancellationToken)
    {
        var entity = new DeclarativeConfigEntity
        {
            ConfigId = config.ConfigId,
            ExtensionId = config.ExtensionId,
            Version = config.Version,
            Config = config.Config,
            InputSchema = config.InputSchema,
            OutputSchema = config.OutputSchema,
            CreatedAt = config.CreatedAt
        };

        await _dbContext.DeclarativeConfigs.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DeclarativeConfigRecord ToRecord(DeclarativeConfigEntity entity)
    {
        return new DeclarativeConfigRecord(
            entity.ConfigId,
            entity.ExtensionId,
            entity.Version,
            entity.Config,
            entity.InputSchema,
            entity.OutputSchema,
            entity.CreatedAt);
    }
}
