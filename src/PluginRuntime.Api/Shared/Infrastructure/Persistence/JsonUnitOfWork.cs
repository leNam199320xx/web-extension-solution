using System.Collections.Concurrent;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Infrastructure.Persistence;

/// <summary>
/// JSON file-based Unit of Work. Each repository operates independently.
/// Transactions are best-effort (no true ACID with file storage).
/// </summary>
public sealed class JsonUnitOfWork : IUnitOfWork
{
    private readonly string _dataDirectory;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public JsonUnitOfWork(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new JsonRepository<T>(_dataDirectory));
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Task.FromResult(0); // JSON repos save immediately

    public Task BeginTransactionAsync(CancellationToken ct = default)
        => Task.CompletedTask; // No transactions for JSON

    public Task CommitTransactionAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken ct = default)
        => Task.CompletedTask; // Best-effort only

    public void Dispose() { }
}
