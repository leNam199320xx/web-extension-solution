using Microsoft.EntityFrameworkCore;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Infrastructure.Persistence;

namespace PluginRuntime.Infrastructure.Repositories;

public class ExecutionRepository : IExecutionRepository
{
    private readonly PluginRuntimeDbContext _dbContext;

    public ExecutionRepository(PluginRuntimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Execution?> GetByIdAsync(Guid executionId, CancellationToken cancellationToken)
    {
        return await _dbContext.Executions
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId, cancellationToken);
    }

    public async Task AddAsync(Execution execution, CancellationToken cancellationToken)
    {
        await _dbContext.Executions.AddAsync(execution, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Execution execution, CancellationToken cancellationToken)
    {
        _dbContext.Executions.Update(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
