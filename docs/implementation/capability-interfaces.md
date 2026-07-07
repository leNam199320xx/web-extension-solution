# 🔑 Capability Interface Contracts

---

# 1. PURPOSE

Định nghĩa đầy đủ tất cả capability interfaces mà plugin có thể sử dụng.

---

# 2. BASE INTERFACE

```csharp
public interface ICapability
{
    string Name { get; }
    string Version { get; }
}
```

---

# 3. DATABASE CAPABILITY

```csharp
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
```

**Rules:**
- All queries MUST be parameterized (no string concatenation)
- Plugin only sees its own isolated schema/data
- Connection management is handled by Core

---

# 4. NETWORK CAPABILITY

```csharp
public interface INetworkCapability : ICapability
{
    /// <summary>
    /// Send an HTTP request to an approved endpoint.
    /// </summary>
    Task<NetworkResponse> SendAsync(
        NetworkRequest request,
        CancellationToken cancellationToken = default);
}

public record NetworkRequest
{
    public string Url { get; init; } = "";
    public string Method { get; init; } = "GET";
    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>();
    public string? Body { get; init; }
    public int TimeoutMs { get; init; } = 5000;
}

public record NetworkResponse
{
    public int StatusCode { get; init; }
    public string Body { get; init; } = "";
    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>();
}
```

**Rules:**
- Only approved domains (from manifest) are reachable
- No raw socket access
- Core proxies all HTTP calls
- Response size limits enforced

---

# 5. STORAGE CAPABILITY

```csharp
public interface IStorageCapability : ICapability
{
    /// <summary>
    /// Store a blob with a given key.
    /// </summary>
    Task StoreAsync(
        string key,
        ReadOnlyMemory<byte> data,
        StorageMetadata? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a blob by key.
    /// </summary>
    Task<ReadOnlyMemory<byte>?> RetrieveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a blob by key.
    /// </summary>
    Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List keys with optional prefix filter.
    /// </summary>
    Task<IReadOnlyList<string>> ListKeysAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default);
}

public record StorageMetadata
{
    public string? ContentType { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public TimeSpan? Expiration { get; init; }
}
```

**Rules:**
- Plugin can only access its own storage namespace
- Storage path is scoped: `{pluginId}/{key}`
- Size limits per object and per plugin total
- No directory traversal

---

# 6. CACHE CAPABILITY

```csharp
public interface ICacheCapability : ICapability
{
    /// <summary>
    /// Get a cached value by key.
    /// </summary>
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a cached value with optional expiration.
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a cached value.
    /// </summary>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);
}
```

**Rules:**
- Cache keys are namespaced per plugin: `{pluginId}:{key}`
- Max key count per plugin enforced
- Max value size enforced
- Serialization handled by Core (System.Text.Json)

---

# 7. QUEUE CAPABILITY (Future)

```csharp
public interface IQueueCapability : ICapability
{
    Task PublishAsync(
        string topic,
        object message,
        CancellationToken cancellationToken = default);

    Task<QueueMessage?> ConsumeAsync(
        string topic,
        CancellationToken cancellationToken = default);
}

public record QueueMessage
{
    public string MessageId { get; init; } = "";
    public string Topic { get; init; } = "";
    public string Body { get; init; } = "";
    public DateTime Timestamp { get; init; }
}
```

---

# 8. EXTENSION CAPABILITY (Inter-Extension Communication)

```csharp
public interface IExtensionCapability : ICapability
{
    /// <summary>
    /// Invoke another extension by ID.
    /// Requires "extension:invoke:{targetId}" permission in manifest.
    /// Target must be Public, or caller must have active Subscription.
    /// </summary>
    Task<ExtensionInvocationResult> InvokeAsync(
        string targetExtensionId,
        object? input = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a target extension is available and accessible.
    /// Returns false if target is not active, not visible, or no subscription.
    /// </summary>
    Task<bool> CanInvokeAsync(
        string targetExtensionId,
        CancellationToken cancellationToken = default);
}

public record ExtensionInvocationResult
{
    public bool Success { get; init; }
    public JsonElement? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string TargetExecutionId { get; init; } = "";
    public int DurationMs { get; init; }
}
```

**Rules:**
- Caller must declare `extension:invoke:{targetId}` in manifest
- Target visibility (Public/Private/Subscription) is enforced at runtime
- Call depth limited (default max: 3)
- Circular invocation detection active
- Timeout cascades (child limited by parent remaining time)
- Each extension runs with its own permissions (no privilege escalation)

For full inter-extension spec, see `docs/architecture/inter-extension-spec.md`.

---

# 8. CAPABILITY RESOLUTION

```csharp
public interface ICapabilityResolver
{
    /// <summary>
    /// Resolve capabilities for a plugin based on its manifest permissions.
    /// Returns only capabilities explicitly granted.
    /// </summary>
    IReadOnlyDictionary<string, ICapability> Resolve(
        Manifest manifest,
        CancellationToken cancellationToken = default);
}
```

---

# 9. SECURITY ENFORCEMENT

Every capability implementation MUST:

- Validate plugin has permission before executing
- Scope data access to plugin's namespace
- Enforce size/rate limits
- Log all access attempts
- Propagate CancellationToken

---

# 🏁 END
