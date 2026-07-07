using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Core.Interfaces;

public interface IPluginExecutor
{
    Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken);
}
