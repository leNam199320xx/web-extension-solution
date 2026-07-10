using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace PluginRuntime.Api.Modules.Plugins.Controllers;

/// <summary>
/// Prometheus-compatible metrics endpoint.
/// Tracks: total requests per module, error rates, Stripe API latency,
/// package subscription count, background job status.
/// </summary>
[ApiController]
public sealed class MetricsController : ControllerBase
{
    private readonly MetricsCollector _metrics;

    public MetricsController(MetricsCollector metrics)
    {
        _metrics = metrics;
    }

    /// <summary>
    /// Returns metrics in Prometheus text exposition format.
    /// </summary>
    [HttpGet("/metrics")]
    [Produces("text/plain")]
    public IActionResult GetMetrics()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# HELP http_requests_total Total HTTP requests per module");
        sb.AppendLine("# TYPE http_requests_total counter");
        foreach (var (module, count) in _metrics.GetRequestCounts())
        {
            sb.AppendLine($"http_requests_total{{module=\"{module}\"}} {count}");
        }

        sb.AppendLine();
        sb.AppendLine("# HELP http_errors_total Total HTTP errors per module");
        sb.AppendLine("# TYPE http_errors_total counter");
        foreach (var (module, count) in _metrics.GetErrorCounts())
        {
            sb.AppendLine($"http_errors_total{{module=\"{module}\"}} {count}");
        }

        sb.AppendLine();
        sb.AppendLine("# HELP stripe_api_latency_ms Stripe API latency in milliseconds");
        sb.AppendLine("# TYPE stripe_api_latency_ms gauge");
        sb.AppendLine($"stripe_api_latency_ms {_metrics.GetStripeLatency()}");

        sb.AppendLine();
        sb.AppendLine("# HELP package_subscriptions_active Active package subscription count");
        sb.AppendLine("# TYPE package_subscriptions_active gauge");
        sb.AppendLine($"package_subscriptions_active {_metrics.GetActiveSubscriptions()}");

        sb.AppendLine();
        sb.AppendLine("# HELP gateway_notification_failures_total Failed Redis notifications");
        sb.AppendLine("# TYPE gateway_notification_failures_total counter");
        sb.AppendLine($"gateway_notification_failures_total {_metrics.GetNotificationFailures()}");

        return Content(sb.ToString(), "text/plain; charset=utf-8");
    }
}

/// <summary>
/// Singleton metrics collector for the application.
/// Thread-safe counters for Prometheus export.
/// </summary>
public sealed class MetricsCollector
{
    private readonly ConcurrentDictionary<string, long> _requestCounts = new();
    private readonly ConcurrentDictionary<string, long> _errorCounts = new();
    private long _stripeLatencyMs;
    private long _activeSubscriptions;
    private long _notificationFailures;

    public void IncrementRequests(string module)
    {
        _requestCounts.AddOrUpdate(module, 1, (_, count) => count + 1);
    }

    public void IncrementErrors(string module)
    {
        _errorCounts.AddOrUpdate(module, 1, (_, count) => count + 1);
    }

    public void RecordStripeLatency(long ms)
    {
        Interlocked.Exchange(ref _stripeLatencyMs, ms);
    }

    public void SetActiveSubscriptions(long count)
    {
        Interlocked.Exchange(ref _activeSubscriptions, count);
    }

    public void IncrementNotificationFailures()
    {
        Interlocked.Increment(ref _notificationFailures);
    }

    public IEnumerable<KeyValuePair<string, long>> GetRequestCounts() => _requestCounts;
    public IEnumerable<KeyValuePair<string, long>> GetErrorCounts() => _errorCounts;
    public long GetStripeLatency() => Interlocked.Read(ref _stripeLatencyMs);
    public long GetActiveSubscriptions() => Interlocked.Read(ref _activeSubscriptions);
    public long GetNotificationFailures() => Interlocked.Read(ref _notificationFailures);
}
