using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Tenants.DTOs;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;
using PluginRuntime.Api.Shared.ValueObjects;

namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// Implements tenant registration, lifecycle management, and listing.
/// </summary>
public sealed class TenantService : ITenantService
{
    private readonly AppDbContext _db;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IAuditService _auditService;

    public TenantService(AppDbContext db, IDomainEventDispatcher eventDispatcher, IAuditService auditService)
    {
        _db = db;
        _eventDispatcher = eventDispatcher;
        _auditService = auditService;
    }

    public async Task<TenantDto> RegisterAsync(TenantRegistrationRequest request, CancellationToken ct)
    {
        var email = new Email(request.ContactEmail);

        var duplicateExists = await _db.Tenants
            .AnyAsync(t => t.ContactEmail == email, ct);

        if (duplicateExists)
            throw new DomainException($"A tenant with email '{email.Value}' already exists.");

        var freePlan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Type == PlanType.Free, ct)
            ?? throw new DomainException("Free plan not found. Platform configuration error.");

        var tenant = new Tenant(
            tenantId: Guid.NewGuid(),
            name: request.Name,
            contactEmail: email,
            planId: freePlan.PlanId,
            companyName: request.CompanyName,
            isInternal: false);

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        await _eventDispatcher.DispatchAsync(new TenantCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenant.TenantId,
            Name: tenant.Name,
            ContactEmail: tenant.ContactEmail.Value,
            IsInternal: false), ct);

        return TenantDto.FromEntity(tenant);
    }

    public async Task<TenantDto> RegisterInternalAsync(InternalTenantRequest request, CancellationToken ct)
    {
        var email = new Email(request.ContactEmail);

        var duplicateExists = await _db.Tenants
            .AnyAsync(t => t.ContactEmail == email, ct);

        if (duplicateExists)
            throw new DomainException($"A tenant with email '{email.Value}' already exists.");

        var internalPlan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Type == PlanType.Internal, ct)
            ?? throw new DomainException("Internal plan not found. Platform configuration error.");

        var tenant = new Tenant(
            tenantId: Guid.NewGuid(),
            name: request.Name,
            contactEmail: email,
            planId: internalPlan.PlanId,
            companyName: request.CompanyName,
            isInternal: true);

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        await _eventDispatcher.DispatchAsync(new TenantCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenant.TenantId,
            Name: tenant.Name,
            ContactEmail: tenant.ContactEmail.Value,
            IsInternal: true), ct);

        return TenantDto.FromEntity(tenant);
    }

    public async Task SuspendAsync(Guid tenantId, string actorId, string reason, CancellationToken ct)
    {
        var tenant = await LoadTenantAsync(tenantId, ct);
        var previousStatus = tenant.Status.ToString();

        tenant.Suspend();
        await _db.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            tenantId,
            actorId,
            "Suspend",
            "Tenant",
            previousStatus,
            tenant.Status.ToString(),
            reason,
            ct);

        await _eventDispatcher.DispatchAsync(new TenantStatusChanged(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenant.TenantId,
            PreviousStatus: previousStatus,
            NewStatus: tenant.Status.ToString(),
            ActorId: actorId,
            Reason: reason), ct);
    }

    public async Task ReactivateAsync(Guid tenantId, string actorId, string reason, CancellationToken ct)
    {
        var tenant = await LoadTenantAsync(tenantId, ct);
        var previousStatus = tenant.Status.ToString();

        tenant.Reactivate();
        await _db.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            tenantId,
            actorId,
            "Reactivate",
            "Tenant",
            previousStatus,
            tenant.Status.ToString(),
            reason,
            ct);

        await _eventDispatcher.DispatchAsync(new TenantStatusChanged(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenant.TenantId,
            PreviousStatus: previousStatus,
            NewStatus: tenant.Status.ToString(),
            ActorId: actorId,
            Reason: reason), ct);
    }

    public async Task DeleteAsync(Guid tenantId, string actorId, string reason, CancellationToken ct)
    {
        var tenant = await LoadTenantAsync(tenantId, ct);
        var previousStatus = tenant.Status.ToString();

        tenant.Delete();
        await _db.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            tenantId,
            actorId,
            "Delete",
            "Tenant",
            previousStatus,
            tenant.Status.ToString(),
            reason,
            ct);

        await _eventDispatcher.DispatchAsync(new TenantStatusChanged(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenant.TenantId,
            PreviousStatus: previousStatus,
            NewStatus: tenant.Status.ToString(),
            ActorId: actorId,
            Reason: reason), ct);
    }

    public async Task<TenantDto?> GetByIdAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);

        return tenant is null ? null : TenantDto.FromEntity(tenant);
    }

    public async Task<PagedResult<TenantDto>> ListAsync(TenantFilter filter, PaginationParams paging, CancellationToken ct)
    {
        var normalized = paging.Normalize();

        var query = _db.Tenants.AsNoTracking().AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (filter.PlanId.HasValue)
            query = query.Where(t => t.PlanId == filter.PlanId.Value);

        if (filter.IsInternal.HasValue)
            query = query.Where(t => t.IsInternal == filter.IsInternal.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.CreatedAt)
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .Select(t => TenantDto.FromEntity(t))
            .ToListAsync(ct);

        return new PagedResult<TenantDto>
        {
            Items = items,
            Page = normalized.Page,
            PageSize = normalized.Take,
            TotalCount = totalCount
        };
    }

    private async Task<Tenant> LoadTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);

        return tenant ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");
    }
}
