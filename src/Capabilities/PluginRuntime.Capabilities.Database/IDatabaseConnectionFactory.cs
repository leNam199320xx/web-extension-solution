namespace PluginRuntime.Capabilities.Database;

/// <summary>
/// Abstraction for database connection management with connection pooling.
/// Implemented by Infrastructure layer to provide actual DB access.
/// </summary>
public interface IDatabaseConnectionFactory
{
    Task<IReadOnlyList<T>> QueryAsync<T>(Guid pluginId, string sql, object? parameters, CancellationToken cancellationToken);
    Task<int> ExecuteAsync(Guid pluginId, string sql, object? parameters, CancellationToken cancellationToken);
    Task<T?> QuerySingleAsync<T>(Guid pluginId, string sql, object? parameters, CancellationToken cancellationToken);
}
