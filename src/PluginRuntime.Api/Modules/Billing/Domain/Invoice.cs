namespace PluginRuntime.Api.Modules.Billing.Domain;

/// <summary>
/// Monthly invoice with base plan + overage + package subscription line items.
/// </summary>
public sealed class Invoice
{
    public Guid InvoiceId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateOnly BillingPeriodStart { get; private set; }
    public DateOnly BillingPeriodEnd { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal OverageAmount { get; private set; }
    public decimal PackageAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public string? StripeInvoiceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private Invoice() { }

    public static Invoice Create(
        Guid tenantId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        decimal baseAmount,
        decimal overageAmount,
        decimal packageAmount)
    {
        return new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            TenantId = tenantId,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            BaseAmount = baseAmount,
            OverageAmount = overageAmount,
            PackageAmount = packageAmount,
            TotalAmount = baseAmount + overageAmount + packageAmount,
            Status = InvoiceStatus.Pending,
            StripeInvoiceId = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetStripeInvoiceId(string stripeInvoiceId)
    {
        StripeInvoiceId = stripeInvoiceId;
    }

    public void UpdateStatus(InvoiceStatus newStatus)
    {
        Status = newStatus;
    }
}

public enum InvoiceStatus
{
    Pending,
    Paid,
    Failed
}
