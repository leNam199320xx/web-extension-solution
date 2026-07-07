namespace PluginRuntime.Core.Interfaces;

public record RuntimeNodeRecord(
    string NodeId,
    string Hostname,
    string Version,
    string Status,
    DateTime StartedAt,
    DateTime LastHeartbeat);

public interface IRuntimeNodeRepository
{
    Task<RuntimeNodeRecord?> GetByIdAsync(string nodeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RuntimeNodeRecord>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(RuntimeNodeRecord node, CancellationToken cancellationToken);
    Task UpdateAsync(RuntimeNodeRecord node, CancellationToken cancellationToken);
}
