using PluginRuntime.Core.Entities;

namespace PluginRuntime.Core.Interfaces;

public interface IExecutionRepository
{
    Task<Execution?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken);
    Task AddAsync(Execution execution, CancellationToken cancellationToken);
    Task UpdateAsync(Execution execution, CancellationToken cancellationToken);
}
