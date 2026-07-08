using Microsoft.Extensions.Logging;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Infrastructure.Observability;

/// <summary>
/// Records plugin execution results to the database via IExecutionRepository.
/// Provides the production implementation of IObservabilityCollector.
/// </summary>
public class ObservabilityCollector : IObservabilityCollector
{
    private readonly IExecutionRepository _executionRepository;
    private readonly ILogger<ObservabilityCollector> _logger;

    public ObservabilityCollector(IExecutionRepository executionRepository, ILogger<ObservabilityCollector> logger)
    {
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecordExecutionAsync(Execution execution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(execution);

        try
        {
            await _executionRepository.AddAsync(execution, cancellationToken);
        }
        catch (Exception ex)
        {
            // Observability should not crash the pipeline — log and continue
            _logger.LogError(ex, "Failed to record execution {ExecutionId}", execution.ExecutionId);
        }
    }
}
