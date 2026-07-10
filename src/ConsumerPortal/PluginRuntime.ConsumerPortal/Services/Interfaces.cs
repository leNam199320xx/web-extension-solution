using PluginRuntime.ConsumerPortal.Models;
using PluginRuntime.ConsumerPortal.Models.DTOs;
using PluginRuntime.ConsumerPortal.Models.Requests;

namespace PluginRuntime.ConsumerPortal.Services;

public interface ITenantService
{
    Task<ApiResult<TenantDto>> GetCurrentTenantAsync(CancellationToken ct = default);
    Task<ApiResult<TenantRegistrationResult>> RegisterAsync(TenantRegistrationRequest request, CancellationToken ct = default);
    Task<ApiResult<bool>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task<ApiResult<bool>> UpdateNotificationPreferencesAsync(NotificationPreferencesRequest request, CancellationToken ct = default);
}

public interface IPlanService
{
    Task<ApiResult<List<PlanDto>>> GetAllPlansAsync(CancellationToken ct = default);
    Task<ApiResult<PlanChangeResult>> ChangePlanAsync(PlanChangeRequest request, CancellationToken ct = default);
    Task<ApiResult<string>> GetStripeCheckoutUrlAsync(Guid planId, CancellationToken ct = default);
}

public interface IApiKeyService
{
    Task<ApiResult<ApiKeyListDto>> GetKeysAsync(CancellationToken ct = default);
    Task<ApiResult<ApiKeyGenerationResult>> GenerateKeyAsync(GenerateKeyRequest request, CancellationToken ct = default);
    Task<ApiResult<ApiKeyGenerationResult>> RotateKeyAsync(Guid keyId, CancellationToken ct = default);
    Task<ApiResult<bool>> RevokeKeyAsync(Guid keyId, CancellationToken ct = default);
}

public interface IUsageService
{
    Task<ApiResult<List<UsageAggregateDto>>> GetDailyUsageAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<ApiResult<UsageSummaryDto>> GetUsageSummaryAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<ApiResult<DashboardUsageDto>> GetDashboardUsageAsync(CancellationToken ct = default);
}

public interface IInvoiceService
{
    Task<ApiResult<PaginatedResult<InvoiceDto>>> GetInvoicesAsync(int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<ApiResult<InvoiceDetailDto>> GetInvoiceDetailAsync(Guid invoiceId, CancellationToken ct = default);
    Task<ApiResult<Stream>> DownloadInvoicePdfAsync(Guid invoiceId, CancellationToken ct = default);
    Task<ApiResult<BillingSummaryDto>> GetBillingSummaryAsync(CancellationToken ct = default);
}

public interface ISupportService
{
    Task<ApiResult<SupportTicketResult>> SubmitTicketAsync(SupportTicketRequest request, CancellationToken ct = default);
}
