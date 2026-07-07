namespace PluginRuntime.Core.Interfaces;

public interface IObservabilityCollector
{
    Task RecordExecutionAsync(Entities.Execution execution, CancellationToken cancellationToken);
}
