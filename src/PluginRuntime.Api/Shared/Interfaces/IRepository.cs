using System.Linq.Expressions;

namespace PluginRuntime.Api.Shared.Interfaces;

/// <summary>
/// Generic repository interface abstracting persistence.
/// Supports PostgreSQL (EF Core), SQLite (EF Core), and JSON file storage.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task RemoveAsync(T entity, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns an IQueryable for advanced queries (only available with EF Core providers).
    /// JSON provider returns an in-memory queryable.
    /// </summary>
    IQueryable<T> Query();
}
