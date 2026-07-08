using PluginRuntime.Api.Modules.Billing.Domain;
using PluginRuntime.Api.Modules.Billing.DTOs;
using PluginRuntime.Api.Shared.DTOs;

namespace PluginRuntime.Api.Modules.Billing.Services;

/// <summary>
/// Handles monthly invoice generation, status updates, and retrieval.
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Generates monthly invoices for all billable tenants for the previous month.
    /// Calculates base plan amount, overage charges, and package subscription fees.
    /// </summary>
    Task GenerateMonthlyInvoicesAsync(CancellationToken ct);

    /// <summary>
    /// Updates the status of an invoice identified by its Stripe invoice ID.
    /// Used by webhook processing to mark invoices as paid or failed.
    /// </summary>
    Task UpdateInvoiceStatusAsync(string stripeInvoiceId, InvoiceStatus newStatus, CancellationToken ct);

    /// <summary>
    /// Retrieves paginated invoices for a specific tenant.
    /// </summary>
    Task<PagedResult<InvoiceDto>> GetTenantInvoicesAsync(Guid tenantId, PaginationParams paging, CancellationToken ct);
}
