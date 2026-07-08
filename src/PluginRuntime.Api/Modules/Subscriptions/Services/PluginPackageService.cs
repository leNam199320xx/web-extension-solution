using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Modules.Subscriptions.DTOs;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Modules.Subscriptions.Services;

/// <summary>
/// Admin service for plugin package CRUD operations.
/// </summary>
public sealed class PluginPackageService : IPluginPackageService
{
    private readonly AppDbContext _dbContext;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PluginPackageService(AppDbContext dbContext, IDomainEventDispatcher eventDispatcher)
    {
        _dbContext = dbContext;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PluginPackageDto> CreateAsync(CreatePackageRequest request, CancellationToken ct)
    {
        ValidateNameAndPrice(request.Name, request.MonthlyPrice);
        ValidatePluginIds(request.PluginIds);

        var package = PluginPackage.Create(
            request.Name,
            request.Description,
            request.MonthlyPrice,
            request.PluginIds);

        _dbContext.PluginPackages.Add(package);
        await _dbContext.SaveChangesAsync(ct);

        return PluginPackageDto.FromEntity(package);
    }

    public async Task<PluginPackageDto> UpdateAsync(Guid packageId, UpdatePackageRequest request, CancellationToken ct)
    {
        ValidateNameAndPrice(request.Name, request.MonthlyPrice);
        ValidatePluginIds(request.PluginIds);

        var package = await _dbContext.PluginPackages
            .Include(p => p.Plugins)
            .FirstOrDefaultAsync(p => p.PackageId == packageId, ct)
            ?? throw new KeyNotFoundException($"Package with ID '{packageId}' not found.");

        package.Update(request.Name, request.Description, request.MonthlyPrice);

        var (added, removed) = package.UpdatePlugins(request.PluginIds);

        await _dbContext.SaveChangesAsync(ct);

        // Dispatch composition changed event if plugins were modified
        if (added.Count > 0 || removed.Count > 0)
        {
            var domainEvent = new PackageCompositionChanged(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTime.UtcNow,
                PackageId: packageId,
                AddedPluginIds: added,
                RemovedPluginIds: removed);

            await _eventDispatcher.DispatchAsync(domainEvent, ct);
        }

        return PluginPackageDto.FromEntity(package);
    }

    public async Task DeactivateAsync(Guid packageId, CancellationToken ct)
    {
        var package = await _dbContext.PluginPackages
            .FirstOrDefaultAsync(p => p.PackageId == packageId, ct)
            ?? throw new KeyNotFoundException($"Package with ID '{packageId}' not found.");

        // Set status to inactive; existing subscriptions are preserved, new ones prevented
        package.Deactivate();
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<PluginPackageDto>> ListActiveAsync(PaginationParams paging, CancellationToken ct)
    {
        var normalized = paging.Normalize();

        var query = _dbContext.PluginPackages
            .Include(p => p.Plugins)
            .Where(p => p.Status == PackageStatus.Active)
            .OrderBy(p => p.Name);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .ToListAsync(ct);

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
        {
            throw new PackageValidationException(
                "Package name must be between 1 and 200 characters.");
        }

        if (monthlyPrice < 0)
        {
            throw new PackageValidationException(
                "Monthly price must be greater than or equal to zero.");
        }
    }

    private static void ValidatePluginIds(IReadOnlyList<Guid> pluginIds)
    {
        // TODO: When the Plugins module is fully implemented, validate that all plugin IDs
        // exist in the plugins table and have Active status. For now, only validate that
        // the IDs are non-empty GUIDs.
        var invalidIds = pluginIds
            .Where(id => id == Guid.Empty)
            .ToList();

        if (invalidIds.Count > 0)
        {
            throw new PackageValidationException(
                $"One or more plugin IDs are invalid (empty GUID). Invalid count: {invalidIds.Count}");
        }
    }
}
