namespace PluginRuntime.Core.Interfaces;

public record DeclarativeConfigRecord(
    Guid ConfigId,
    string ExtensionId,
    string Version,
    string Config,
    string? InputSchema,
    string? OutputSchema,
    DateTime CreatedAt);

public interface IDeclarativeConfigRepository
{
    Task<DeclarativeConfigRecord?> GetByIdAsync(Guid configId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeclarativeConfigRecord>> GetByExtensionIdAsync(string extensionId, CancellationToken cancellationToken);
    Task AddAsync(DeclarativeConfigRecord config, CancellationToken cancellationToken);
}
