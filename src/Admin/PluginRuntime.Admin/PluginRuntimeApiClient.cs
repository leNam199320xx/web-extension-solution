using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PluginRuntime.Admin;

/// <summary>
/// Typed HttpClient for communicating with PluginRuntime.Api.
/// Automatically attaches Bearer JWT token to all requests.
/// </summary>
public sealed class PluginRuntimeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthTokenProvider _tokenProvider;

    public PluginRuntimeApiClient(HttpClient httpClient, AuthTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    // ---------------------------------------------------------------
    // Internal helper — attach token before every call
    // ---------------------------------------------------------------
    private void SetAuth()
    {
        var token = _tokenProvider.GetToken();
        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    // ---------------------------------------------------------------
    // Dashboard metrics
    // ---------------------------------------------------------------
    public async Task<DashboardMetrics?> GetDashboardMetricsAsync(CancellationToken ct = default)
    {
        SetAuth();
        return await _httpClient.GetFromJsonAsync<DashboardMetrics>("api/v1/admin/metrics", ct);
    }

    // ---------------------------------------------------------------
    // Plugins
    // ---------------------------------------------------------------
    public async Task<List<PluginSummary>?> GetPluginsAsync(CancellationToken ct = default)
    {
        SetAuth();
        return await _httpClient.GetFromJsonAsync<List<PluginSummary>>("api/v1/plugins", ct);
    }

    // ---------------------------------------------------------------
    // Approvals
    // ---------------------------------------------------------------
    public async Task<List<ApprovalItem>?> GetApprovalsAsync(string status = "Pending", CancellationToken ct = default)
    {
        SetAuth();
        return await _httpClient.GetFromJsonAsync<List<ApprovalItem>>(
            $"api/v1/approvals?status={Uri.EscapeDataString(status)}", ct);
    }

    public async Task<PermissionReviewDetail?> GetPermissionReviewAsync(Guid versionId, CancellationToken ct = default)
    {
        SetAuth();
        return await _httpClient.GetFromJsonAsync<PermissionReviewDetail>(
            $"api/v1/approvals/{versionId}/permissions", ct);
    }

    public async Task<bool> ApproveVersionAsync(Guid versionId, string? comment, CancellationToken ct = default)
    {
        SetAuth();
        var resp = await _httpClient.PostAsJsonAsync(
            $"api/v1/approvals/{versionId}/approve",
            new { comment, decision = "Approved" }, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RejectVersionAsync(Guid versionId, string? comment, CancellationToken ct = default)
    {
        SetAuth();
        var resp = await _httpClient.PostAsJsonAsync(
            $"api/v1/approvals/{versionId}/reject",
            new { comment, decision = "Rejected" }, ct);
        return resp.IsSuccessStatusCode;
    }

    // ---------------------------------------------------------------
    // Executions (Monitoring)
    // ---------------------------------------------------------------
    public async Task<PagedResult<ExecutionSummary>?> GetExecutionsAsync(
        string? pluginName = null,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        SetAuth();
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["pluginName"] = pluginName,
            ["status"] = status,
            ["from"] = from?.ToString("O"),
            ["to"] = to?.ToString("O"),
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        });
        return await _httpClient.GetFromJsonAsync<PagedResult<ExecutionSummary>>(
            $"api/v1/executions{query}", ct);
    }

    // ---------------------------------------------------------------
    // Audit logs
    // ---------------------------------------------------------------
    public async Task<PagedResult<AuditLogItem>?> GetAuditLogsAsync(
        string? actorId = null,
        string? action = null,
        string? resourceType = null,
        DateTime? from = null,
        DateTime? to = null,
        string? result = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        SetAuth();
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["actorId"] = actorId,
            ["action"] = action,
            ["resourceType"] = resourceType,
            ["from"] = from?.ToString("O"),
            ["to"] = to?.ToString("O"),
            ["result"] = result,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        });
        return await _httpClient.GetFromJsonAsync<PagedResult<AuditLogItem>>(
            $"api/v1/audit{query}", ct);
    }

    // ---------------------------------------------------------------
    // Extensions
    // ---------------------------------------------------------------
    public async Task<List<ExtensionItem>?> GetExtensionsAsync(CancellationToken ct = default)
    {
        SetAuth();
        return await _httpClient.GetFromJsonAsync<List<ExtensionItem>>("api/v1/extensions", ct);
    }

    // ---------------------------------------------------------------
    // Health
    // ---------------------------------------------------------------
    public async Task<HealthStatus?> GetHealthAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<HealthStatus>("health", ct);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private static string BuildQuery(Dictionary<string, string?> parameters)
    {
        var parts = parameters
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}");
        var qs = string.Join("&", parts);
        return string.IsNullOrEmpty(qs) ? "" : $"?{qs}";
    }
}

// ---------------------------------------------------------------
// DTO records used by the Admin portal
// ---------------------------------------------------------------

public record DashboardMetrics(
    int ActivePluginCount,
    long TotalExecutionCount,
    double ErrorRatePercent,
    double CpuPercent,
    double MemoryPercent,
    int RunningExecutions);

public record PluginSummary(
    Guid PluginId,
    string Name,
    string DisplayName,
    string Status,
    DateTime CreatedAt);

public record ApprovalItem(
    Guid VersionId,
    Guid PluginId,
    string PluginName,
    string Version,
    string Author,
    DateTime UploadedAt,
    string RiskLevel,
    string Status);

public record PermissionReviewDetail(
    Guid VersionId,
    object Permissions,
    object RiskSummary,
    object? PermissionDiff,
    string OverallRiskLevel);

public record ExecutionSummary(
    Guid ExecutionId,
    Guid PluginId,
    string? PluginName,
    string Status,
    int? DurationMs,
    string TraceId,
    DateTime StartTime);

public record AuditLogItem(
    Guid AuditId,
    DateTime Timestamp,
    string ActorId,
    string ActorType,
    string Action,
    string ResourceType,
    string ResourceId,
    string Result,
    string? IpAddress);

public record ExtensionItem(
    string ExtensionId,
    string DisplayName,
    string Visibility,
    string? Category,
    int SubscriberCount,
    DateTime UpdatedAt);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record HealthStatus(
    string Status,
    Dictionary<string, string> Checks);
