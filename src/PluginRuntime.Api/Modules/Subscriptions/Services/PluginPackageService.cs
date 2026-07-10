using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Admin service for plugin package CRUD using IRepository.
/// </summary>
public sealed class PluginPackageService : IPluginPackageService
{
    private readonly IRepository<PluginPackage> _packages;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PluginPackageService(IRepository<PluginPackage> packages, IDomainEventDispatcher eventDispatcher)
    {
        _packages = packages;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PluginPackageDto> CreateAsync(CreatePackageRequest request, CancellationToken ct)
    {
        ValidateNameAndPrice(request.Name, request.MonthlyPrice);
        ValidatePluginIds(request.PluginIds);

        var package = PluginPackage.Create(request.Name, request.Description, request.MonthlyPrice, request.PluginIds);

        await _packages.AddAsync(package, ct);
        await _packages.SaveChangesAsync(ct);

        return PluginPackageDto.FromEntity(package);
    }

    public async Task<PluginPackageDto> UpdateAsync(Guid packageId, UpdatePackageRequest request, CancellationToken ct)
    {
        ValidateNameAndPrice(request.Name, request.MonthlyPrice);
        ValidatePluginIds(request.PluginIds);

        var package = await _packages.GetByIdAsync(packageId, ct)
            ?? throw new KeyNotFoundException($"Package with ID '{packageId}' not found.");

        package.Update(request.Name, request.Description, request.MonthlyPrice);
        var (added, removed) = package.UpdatePlugins(request.PluginIds);

        await _packages.UpdateAsync(package, ct);
        await _packages.SaveChangesAsync(ct);

        if (added.Count > 0 || removed.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(new PackageCompositionChanged(
                Guid.NewGuid(), DateTime.UtcNow, packageId, added, removed), ct);
        }

        return PluginPackageDto.FromEntity(package);
    }

    public async Task DeactivateAsync(Guid packageId, CancellationToken ct)
    {
        var package = await _packages.GetByIdAsync(packageId, ct)
            ?? throw new KeyNotFoundException($"Package with ID '{packageId}' not found.");

        package.Deactivate();
        await _packages.UpdateAsync(package, ct);
        await _packages.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<PluginPackageDto>> ListActiveAsync(PaginationParams paging, CancellationToken ct)
    {
        var normalized = paging.Normalize();
        var active = await _packages.FindAsync(p => p.Status == PackageStatus.Active, ct);

        var ordered = active.OrderBy(p => p.Name).ToList();
        var totalCount = ordered.Count;
        var items = ordered.Skip(normalized.Skip).Take(normalized.Take).ToList();

        return new PagedResult<PluginPackageDto>
        {
            Items = items.Select(PluginPackageDto.FromEntity).ToList(),
            Page = normalized.Page,
            PageSize = normalized.PageSize,
            TotalCount = totalCount
        };
    }

    private static void ValidateNameAndPrice(string name, decimal monthlyPrice)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new PackageValidationException("Package name must be between 1 and 200 characters.");
        if (monthlyPrice < 0)
            throw new PackageValidationException("Monthly price must be >= 0.");
    }

    private static void ValidatePluginIds(IReadOnlyList<Guid> pluginIds)
    {
        var invalidIds = pluginIds.Where(id => id == Guid.Empty).ToList();
        if (invalidIds.Count > 0)
            throw new PackageValidationException($"One or more plugin IDs are invalid (empty GUID).");
    }
}
