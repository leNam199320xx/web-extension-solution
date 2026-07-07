using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Capabilities.Abstractions;

public interface INetworkCapability : ICapability
{
    /// <summary>
    /// Send an HTTP request to an approved endpoint.
    /// </summary>
    Task<NetworkResponse> SendAsync(
        NetworkRequest request,
        CancellationToken cancellationToken = default);
}
