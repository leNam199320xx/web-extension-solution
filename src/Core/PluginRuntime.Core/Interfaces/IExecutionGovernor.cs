using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Core.Interfaces;

public interface IExecutionGovernor
{
    Task<T> ExecuteWithLimitsAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ResourceLimits limits,
        CancellationToken cancellationToken);
}
