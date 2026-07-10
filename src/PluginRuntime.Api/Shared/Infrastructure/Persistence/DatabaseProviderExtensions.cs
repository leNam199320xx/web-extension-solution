using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Infrastructure.Persistence;

/// <summary>
/// Extension methods to register the appropriate database provider based on configuration.
/// Supported providers: "PostgreSQL", "SQLite", "Json"
/// </summary>
public static class DatabaseProviderExtensions
{
    /// <summary>
    /// Registers the database provider based on the "DatabaseProvider" configuration value.
    /// Defaults to "PostgreSQL" if not specified.
    /// </summary>
    public static IServiceCollection AddDatabaseProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = (configuration["DatabaseProvider"] ?? "PostgreSQL").Trim();

        switch (provider.ToLowerInvariant())
        {
            case "postgresql":
            case "postgres":
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("Default")));
                services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
                break;

            case "sqlite":
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=pluginruntime.db"));
                services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
                break;

            case "json":
                var dataDir = configuration["JsonDataDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "data");
                services.AddSingleton<IUnitOfWork>(_ => new JsonUnitOfWork(dataDir));
                // Register a factory for open-generic resolution
                services.AddSingleton(typeof(IRepository<>), typeof(JsonRepositoryFactory<>));
                // Register an in-memory DbContext so HealthController and other EF-dependent code still compiles
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("PluginRuntime_Json"));
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider: '{provider}'. Supported: PostgreSQL, SQLite, Json");
        }

        return services;
    }
}

/// <summary>
/// Factory wrapper for resolving JsonRepository&lt;T&gt; as open-generic in DI.
/// </summary>
internal sealed class JsonRepositoryFactory<T> : IRepository<T> where T : class
{
    private readonly JsonRepository<T> _inner;

    public JsonRepositoryFactory(IConfiguration configuration)
    {
        var dataDir = configuration["JsonDataDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "data");
        _inner = new JsonRepository<T>(dataDir);
    }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => _inner.GetByIdAsync(id, ct);
    public Task<List<T>> GetAllAsync(CancellationToken ct = default) => _inner.GetAllAsync(ct);
    public Task<List<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default) => _inner.FindAsync(predicate, ct);
    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) => _inner.CountAsync(predicate, ct);
    public Task AddAsync(T entity, CancellationToken ct = default) => _inner.AddAsync(entity, ct);
    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) => _inner.AddRangeAsync(entities, ct);
    public Task UpdateAsync(T entity, CancellationToken ct = default) => _inner.UpdateAsync(entity, ct);
    public Task RemoveAsync(T entity, CancellationToken ct = default) => _inner.RemoveAsync(entity, ct);
    public Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) => _inner.RemoveRangeAsync(entities, ct);
    public Task SaveChangesAsync(CancellationToken ct = default) => _inner.SaveChangesAsync(ct);
    public IQueryable<T> Query() => _inner.Query();
}
