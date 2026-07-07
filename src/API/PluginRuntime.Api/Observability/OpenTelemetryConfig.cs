using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace PluginRuntime.Api.Observability;

public static class OpenTelemetryConfig
{
    public const string ServiceName = "PluginRuntime";
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    public static IServiceCollection AddPluginRuntimeTracing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddOtlpExporter(opts =>
                    {
                        var endpoint = configuration["OpenTelemetry:Endpoint"];
                        if (!string.IsNullOrEmpty(endpoint))
                            opts.Endpoint = new Uri(endpoint);
                    });
            });

        return services;
    }
}
