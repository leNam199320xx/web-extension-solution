using PluginRuntime.Api.Modules.Billing.Domain;

namespace PluginRuntime.Api.Modules.Billing.DTOs;

/// <summary>
/// Read model for an invoice returned from API endpoints.
/// </summary>
public sealed record InvoiceDto
{
    public Guid InvoiceId { get; init; }
    public Guid TenantId { get; init; }
    public DateOnly BillingPeriodStart { get; init; }
    public DateOnly BillingPeriodEnd { get; init; }
    public decimal BaseAmount { get; init; }
    public decimal OverageAmount { get; init; }
    public decimal PackageAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? StripeInvoiceId { get; init; }
    public DateTime CreatedAt { get; init; }

    public static InvoiceDto FromEntity(Invoice entity) => new()
    {
        InvoiceId = entity.InvoiceId,
        TenantId = entity.TenantId,
        BillingPeriodStart = entity.BillingPeriodStart,
        BillingPeriodEnd = entity.BillingPeriodEnd,
        BaseAmount = entity.BaseAmount,
        OverageAmount = entity.OverageAmount,
        PackageAmount = entity.PackageAmount,
        TotalAmount = entity.TotalAmount,
        Status = entity.Status.ToString(),
        StripeInvoiceId = entity.StripeInvoiceId,
        CreatedAt = entity.CreatedAt
    };
}
