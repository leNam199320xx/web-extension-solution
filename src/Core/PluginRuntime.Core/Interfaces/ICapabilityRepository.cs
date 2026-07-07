namespace PluginRuntime.Core.Interfaces;

public record CapabilityRecord(
    Guid CapabilityId,
    string Name,
    string Version,
    string Category,
    string? Description,
    bool Enabled,
    DateTime CreatedAt);

public interface ICapabilityRepository
{
    Task<CapabilityRecord?> GetByIdAsync(Guid capabilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CapabilityRecord>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(CapabilityRecord capability, CancellationToken cancellationToken);
}
