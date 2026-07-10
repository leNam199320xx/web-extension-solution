using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Shared.Infrastructure.Persistence;

/// <summary>
/// JSON file-based repository. Stores each entity type as a separate JSON file.
/// Thread-safe using ConcurrentDictionary and file locking.
/// Suitable for development, demos, and embedded deployments with low concurrency.
/// </summary>
public sealed class JsonRepository<T> : IRepository<T> where T : class
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    static JsonRepository()
    {
        // Enable serialization of properties with private setters
        JsonOptions.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                static typeInfo =>
                {
                    if (typeInfo.Kind != System.Text.Json.Serialization.Metadata.JsonTypeInfoKind.Object) return;
                    foreach (var prop in typeInfo.Properties)
                    {
                        if (!prop.IsRequired) continue;
                    }
                    // Force all properties to be settable during deserialization
                    foreach (var prop in typeInfo.Properties)
                    {
                        if (prop.Set is null)
                        {
                            var propertyInfo = typeof(T).GetProperty(prop.Name,
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (propertyInfo?.SetMethod is not null)
                            {
                                prop.Set = (obj, value) => propertyInfo.SetMethod.Invoke(obj, [value]);
                            }
                        }
                    }
                }
            }
        };
    }

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<T>? _cache;

    public JsonRepository(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, $"{typeof(T).Name}.json");
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var items = await LoadAsync(ct);
        return items.FirstOrDefault(e => GetId(e) == id);
    }

    public async Task<List<T>> GetAllAsync(CancellationToken ct = default)
        => await LoadAsync(ct);

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        var items = await LoadAsync(ct);
        return items.AsQueryable().Where(predicate).ToList();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        var items = await LoadAsync(ct);
        return predicate is null ? items.Count : items.AsQueryable().Count(predicate);
    }

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        var items = await LoadAsync(ct);
        items.Add(entity);
        await SaveAsync(items, ct);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        var items = await LoadAsync(ct);
        items.AddRange(entities);
        await SaveAsync(items, ct);
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        var items = await LoadAsync(ct);
        var id = GetId(entity);
        var index = items.FindIndex(e => GetId(e) == id);
        if (index >= 0) items[index] = entity;
        await SaveAsync(items, ct);
    }

    public async Task RemoveAsync(T entity, CancellationToken ct = default)
    {
        var items = await LoadAsync(ct);
        var id = GetId(entity);
        items.RemoveAll(e => GetId(e) == id);
        await SaveAsync(items, ct);
    }

    public async Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        var items = await LoadAsync(ct);
        var idsToRemove = entities.Select(GetId).ToHashSet();
        items.RemoveAll(e => idsToRemove.Contains(GetId(e)));
        await SaveAsync(items, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        // JSON repository saves immediately on each operation, no-op here
        await Task.CompletedTask;
    }

    public IQueryable<T> Query()
    {
        var items = LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        return items.AsQueryable();
    }

    private async Task<List<T>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null) return _cache;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cache is not null) return _cache;

            if (!File.Exists(_filePath))
            {
                _cache = [];
                return _cache;
            }

            var json = await File.ReadAllTextAsync(_filePath, ct);
            _cache = JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveAsync(List<T> items, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _cache = items;
            var json = JsonSerializer.Serialize(items, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static Guid GetId(T entity)
    {
        // Convention: look for a property ending with "Id" that is a Guid
        var idProp = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType == typeof(Guid) &&
                (p.Name == "Id" || p.Name == $"{typeof(T).Name}Id" || p.Name.EndsWith("Id")));

        if (idProp is null)
            throw new InvalidOperationException($"Entity {typeof(T).Name} has no Guid Id property.");

        return (Guid)idProp.GetValue(entity)!;
    }
}
