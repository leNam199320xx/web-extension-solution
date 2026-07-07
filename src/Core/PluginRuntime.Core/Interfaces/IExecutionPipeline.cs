using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Core.Interfaces;

public interface IExecutionPipeline
{
    Task<ExecutionResult> ProcessAsync(ExecutionRequest request, CancellationToken cancellationToken);
}
