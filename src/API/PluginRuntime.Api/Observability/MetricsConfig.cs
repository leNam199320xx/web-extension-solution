using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

namespace PluginRuntime.Api.Observability;

public static class MetricsConfig
{
    public const string MeterName = "PluginRuntime.Metrics";

    private static readonly Meter Meter = new(MeterName);

    // Counters
    public static readonly Counter<long> ExecutionTotal = Meter.CreateCounter<long>(
        "plugin_execution_total", description: "Total plugin executions by status");

    public static readonly Counter<long> TimeoutTotal = Meter.CreateCounter<long>(
        "plugin_timeout_total", description: "Total plugin execution timeouts");

    public static readonly Counter<long> SignatureFailures = Meter.CreateCounter<long>(
        "security_signature_failures_total", description: "Total signature verification failures");

    public static readonly Counter<long> CapabilityDenied = Meter.CreateCounter<long>(
        "security_capability_denied_total", description: "Total capability access denials");

    public static readonly Counter<long> RevokedAttempts = Meter.CreateCounter<long>(
        "security_revoked_execution_attempts", description: "Total attempts to execute revoked plugins");

    // Histograms
    public static readonly Histogram<double> ExecutionDuration = Meter.CreateHistogram<double>(
        "plugin_execution_duration_ms", "ms", "Plugin execution duration in milliseconds");

    public static readonly Histogram<double> MemoryUsage = Meter.CreateHistogram<double>(
        "plugin_memory_usage_mb", "MB", "Plugin memory usage in megabytes");

    // Gauge (using UpDownCounter as gauge equivalent in OpenTelemetry)
    public static readonly UpDownCounter<long> ExecutionActive = Meter.CreateUpDownCounter<long>(
        "plugin_execution_active", description: "Currently active plugin executions");

    public static IServiceCollection AddPluginRuntimeMetrics(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }
}
