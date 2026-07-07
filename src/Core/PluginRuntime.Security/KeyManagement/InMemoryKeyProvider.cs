using System.Collections.Concurrent;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Security.KeyManagement;

public class InMemoryKeyProvider : IKeyProvider
{
    private readonly ConcurrentDictionary<string, byte[]> _keys = new();

    public InMemoryKeyProvider() { }

    public InMemoryKeyProvider(IDictionary<string, byte[]> keys)
    {
        foreach (var kvp in keys)
        {
            _keys[kvp.Key] = kvp.Value;
        }
    }

    public Task<byte[]?> GetPublicKeyAsync(string keyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _keys.TryGetValue(keyId, out var key);
        return Task.FromResult(key);
    }

    public Task<IReadOnlyList<string>> ListKeyIdsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> keys = _keys.Keys.ToList();
        return Task.FromResult(keys);
    }

    public void AddKey(string keyId, byte[] publicKey)
    {
        _keys[keyId] = publicKey;
    }

    public void RemoveKey(string keyId)
    {
        _keys.TryRemove(keyId, out _);
    }
}
