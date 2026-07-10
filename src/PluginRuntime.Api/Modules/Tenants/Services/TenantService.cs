using PluginRuntime.Api.Modules.Tenants.DTOs;
using PluginRuntime.Api.Shared.DTOs;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.Interfaces;
using PluginRuntime.Api.Shared.ValueObjects;

namespace PluginRuntime.Api.Modules.Tenants.Services;

/// <summary>
/// Implements tenant registration, lifecycle management, and listing.
/// Uses IRepository for provider-agnostic persistence (PostgreSQL, SQLite, or JSON).
/// </summary>
public sealed class TenantService : ITenantService
{
    private readonly IRepository<Tenant> _tenants;
    private readonly IRepository<Plan> _plans;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IAuditService _auditService;

    public TenantService(
        IRepository<Tenant> tenants,
        IRepository<Plan> plans,
        IDomainEventDispatcher eventDispatcher,
        IAuditService auditService)
    {
        _tenants = tenants;
        _plans = plans;
        _eventDispatcher = eventDispatcher;
        _auditService = auditService;
    }

    public async Task<TenantDto> RegisterAsync(TenantRegistrationRequest request, CancellationToken ct)
    {
        var email = new Email(request.ContactEmail);

        var duplicates = await _tenants.FindAsync(t => t.ContactEmail == email, ct);
        if (duplicates.Count > 0)
            throw new DomainException($"A tenant with email '{email.Value}' already exists.");

        var plans = await _plans.FindAsync(p => p.Type == PlanType.Free, ct);
        var freePlan = plans.FirstOrDefault()
            ?? throw new DomainException("Free plan not found. Platform configuration error.");

        var tenant = new Tenant(
            tenantId: Guid.NewGuid(),
            name: request.Name,
            contactEmail: email,
            planId: freePlan.PlanId,
            companyName: request.CompanyName,
            isInternal: false);

        await _tenants.AddAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);

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

        var duplicates = await _tenants.FindAsync(t => t.ContactEmail == email, ct);
        if (duplicates.Count > 0)
            throw new DomainException($"A tenant with email '{email.Value}' already exists.");

        var plans = await _plans.FindAsync(p => p.Type == PlanType.Internal, ct);
        var internalPlan = plans.FirstOrDefault()
            ?? throw new DomainException("Internal plan not found. Platform configuration error.");

        var tenant = new Tenant(
            tenantId: Guid.NewGuid(),
            name: request.Name,
            contactEmail: email,
            planId: internalPlan.PlanId,
            companyName: request.CompanyName,
            isInternal: true);

        await _tenants.AddAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);

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
        await _tenants.UpdateAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);

        await _auditService.LogAsync(tenantId, actorId, "Suspend", "Tenant", previousStatus, tenant.Status.ToString(), reason, ct);

        await _eventDispatcher.DispatchAsync(new TenantStatusChanged(
            EventId: Guid.NewGuid(), OccurredAt: DateTime.UtcNow, TenantId: tenant.TenantId,
            PreviousStatus: previousStatus, NewStatus: tenant.Status.ToString(), ActorId: actorId, Reason: reason), ct);
    }

    public async Task ReactivateAsync(Guid tenantId, string actorId, string reason, CancellationToken ct)
    {
        var tenant = await LoadTenantAsync(tenantId, ct);
        var previousStatus = tenant.Status.ToString();

        tenant.Reactivate();
        await _tenants.UpdateAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);

        await _auditService.LogAsync(tenantId, actorId, "Reactivate", "Tenant", previousStatus, tenant.Status.ToString(), reason, ct);

        await _eventDispatcher.DispatchAsync(new TenantStatusChanged(
            EventId: Guid.NewGuid(), OccurredAt: DateTime.UtcNow, TenantId: tenant.TenantId,
            PreviousStatus: previousStatus, NewStatus: tenant.Status.ToString(), ActorId: actorId, Reason: reason), ct);
    }

    public async Task DeleteAsync(Guid tenantId, string actorId, string reason, CancellationToken ct)
    {
        var tenant = await LoadTenantAsync(tenantId, ct);
        var previousStatus = tenant.Status.ToString();

        tenant.Delete();
        await _tenants.UpdateAsync(tenant, ct);
        await _tenants.SaveChangesAsync(ct);

        await _auditService.LogAsync(tenantId, actorId, "Delete", "Tenant", previousStatus, tenant.Status.ToString(), reason, ct);

        await _eventDispatcher.DispatchAsync(new TenantStatusChanged(
            EventId: Guid.NewGuid(), OccurredAt: DateTime.UtcNow, TenantId: tenant.TenantId,
            PreviousStatus: previousStatus, NewStatus: tenant.Status.ToString(), ActorId: actorId, Reason: reason), ct);
    }

    public async Task<TenantDto?> GetByIdAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        return tenant is null ? null : TenantDto.FromEntity(tenant);
    }

    public async Task<PagedResult<TenantDto>> ListAsync(TenantFilter filter, PaginationParams paging, CancellationToken ct)
    {
        var normalized = paging.Normalize();

        var query = _tenants.Query();

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);
        if (filter.PlanId.HasValue)
            query = query.Where(t => t.PlanId == filter.PlanId.Value);
        if (filter.IsInternal.HasValue)
            query = query.Where(t => t.IsInternal == filter.IsInternal.Value);

        var totalCount = query.Count();
        var items = query
            .OrderBy(t => t.CreatedAt)
            .Skip(normalized.Skip)
            .Take(normalized.Take)
            .ToList()
            .Select(TenantDto.FromEntity)
            .ToList();

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
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        return tenant ?? throw new DomainException($"Tenant with ID '{tenantId}' not found.");
    }
}
