using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Abstractions;

public interface IDatabaseCapability : ICapability
{
    /// <summary>
    /// Execute a parameterized query and return results.
    /// </summary>
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a parameterized command (INSERT, UPDATE, DELETE).
    /// Returns affected row count.
    /// </summary>
    Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a query and return a single result or default.
    /// </summary>
    Task<T?> QuerySingleAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);
}
