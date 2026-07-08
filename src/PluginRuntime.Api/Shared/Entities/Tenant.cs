using PluginRuntime.Api.Shared.Exceptions;
using PluginRuntime.Api.Shared.ValueObjects;

namespace PluginRuntime.Api.Shared.Entities;

/// <summary>
/// Tenant entity representing an organizational account on the platform.
/// Uses private setters and domain methods to enforce invariants.
/// </summary>
public sealed class Tenant
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Email ContactEmail { get; private set; } = null!;
    public string? CompanyName { get; private set; }
    public TenantStatus Status { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid? PendingPlanId { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public bool IsInternal { get; private set; }
    public long Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private Tenant() { }

    public Tenant(
        Guid tenantId,
        string name,
        Email contactEmail,
        Guid planId,
        string? companyName = null,
        bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tenant name cannot be empty.");

        if (name.Length > 200)
            throw new DomainException("Tenant name cannot exceed 200 characters.");

        TenantId = tenantId;
        Name = name;
        ContactEmail = contactEmail ?? throw new DomainException("Contact email is required.");
        CompanyName = companyName;
        Status = TenantStatus.Active;
        PlanId = planId;
        IsInternal = isInternal;
        Version = 1;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns a new plan to the tenant. Clears any pending plan.
    /// </summary>
    public void AssignPlan(Guid planId)
    {
        if (Status == TenantStatus.Deleted)
            throw new DomainException("Cannot assign plan to a deleted tenant.");

        PlanId = planId;
        PendingPlanId = null;
        IncrementVersion();
    }

    /// <summary>
    /// Schedules a pending plan change (for downgrades that take effect at next billing cycle).
    /// </summary>
    public void SetPendingPlan(Guid pendingPlanId)
    {
        if (Status == TenantStatus.Deleted)
            throw new DomainException("Cannot set pending plan for a deleted tenant.");

        PendingPlanId = pendingPlanId;
        IncrementVersion();
    }

    /// <summary>
    /// Suspends the tenant. Suspended tenants cannot access platform services.
    /// </summary>
    public void Suspend()
    {
        if (Status != TenantStatus.Active)
            throw new DomainException($"Cannot suspend tenant with status '{Status}'. Only active tenants can be suspended.");

        Status = TenantStatus.Suspended;
        IncrementVersion();
    }

    /// <summary>
    /// Reactivates a suspended tenant.
    /// </summary>
    public void Reactivate()
    {
        if (Status != TenantStatus.Suspended)
            throw new DomainException($"Cannot reactivate tenant with status '{Status}'. Only suspended tenants can be reactivated.");

        Status = TenantStatus.Active;
        IncrementVersion();
    }

    /// <summary>
    /// Soft-deletes the tenant. Deleted tenants cannot be reactivated.
    /// </summary>
    public void Delete()
    {
        if (Status == TenantStatus.Deleted)
            throw new DomainException("Tenant is already deleted.");

        Status = TenantStatus.Deleted;
        IncrementVersion();
    }

    /// <summary>
    /// Sets the Stripe customer ID after Stripe customer creation.
    /// </summary>
    public void SetStripeCustomerId(string stripeCustomerId)
    {
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
            throw new DomainException("Stripe customer ID cannot be empty.");

        StripeCustomerId = stripeCustomerId;
    }

    /// <summary>
    /// Sets the Stripe subscription ID.
    /// </summary>
    public void SetStripeSubscriptionId(string stripeSubscriptionId)
    {
        if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            throw new DomainException("Stripe subscription ID cannot be empty.");

        StripeSubscriptionId = stripeSubscriptionId;
    }

    private void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
