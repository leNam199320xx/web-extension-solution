using System.Text.Json;

namespace PluginRuntime.Api.Observability;

/// <summary>
/// Configures structured JSON logging with fields required for observability:
/// timestamp (ISO 8601), level, traceId, executionId, pluginId, correlationId,
/// userId, tenantId, event name, and message.
/// </summary>
public static class LoggingConfig
{
    public static IServiceCollection AddStructuredJsonLogging(this IServiceCollection services, ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.JsonWriterOptions = new JsonWriterOptions
            {
                Indented = false
            };
        });

        return services;
    }
}
