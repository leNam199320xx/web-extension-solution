using System.Net.Http.Json;
using PluginRuntime.ConsumerPortal.Models;
using PluginRuntime.ConsumerPortal.Models.DTOs;

namespace PluginRuntime.ConsumerPortal.Services;

public sealed class InvoiceService : IInvoiceService
{
    private readonly HttpClient _http;
    public InvoiceService(HttpClient http) => _http = http;

    public async Task<ApiResult<PaginatedResult<InvoiceDto>>> GetInvoicesAsync(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<PaginatedResult<InvoiceDto>>(
                $"api/billing/invoices?page={page}&pageSize={pageSize}", ct);
            return result is not null
                ? ApiResult<PaginatedResult<InvoiceDto>>.Success(result)
                : ApiResult<PaginatedResult<InvoiceDto>>.Success(new PaginatedResult<InvoiceDto>([], 0, page, pageSize));
        }
        catch (HttpRequestException) { return ApiResult<PaginatedResult<InvoiceDto>>.NetworkFailure(); }
    }

    public async Task<ApiResult<InvoiceDetailDto>> GetInvoiceDetailAsync(Guid invoiceId, CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<InvoiceDetailDto>($"api/billing/invoices/{invoiceId}", ct);
            return result is not null
                ? ApiResult<InvoiceDetailDto>.Success(result)
                : ApiResult<InvoiceDetailDto>.Fail(new ApiError("NOT_FOUND", "Invoice not found", null));
        }
        catch (HttpRequestException) { return ApiResult<InvoiceDetailDto>.NetworkFailure(); }
    }

    public async Task<ApiResult<Stream>> DownloadInvoicePdfAsync(Guid invoiceId, CancellationToken ct = default)
    {
        try
        {
            var stream = await _http.GetStreamAsync($"api/billing/invoices/{invoiceId}/pdf", ct);
            return ApiResult<Stream>.Success(stream);
        }
        catch (HttpRequestException) { return ApiResult<Stream>.NetworkFailure(); }
    }

    public async Task<ApiResult<BillingSummaryDto>> GetBillingSummaryAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<BillingSummaryDto>("api/billing/summary", ct);
            return result is not null
                ? ApiResult<BillingSummaryDto>.Success(result)
                : ApiResult<BillingSummaryDto>.Fail(new ApiError("NOT_FOUND", "Billing summary not found", null));
        }
        catch (HttpRequestException) { return ApiResult<BillingSummaryDto>.NetworkFailure(); }
    }
}
