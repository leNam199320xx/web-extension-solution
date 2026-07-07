using System.Diagnostics;

namespace PluginRuntime.Runtime.Telemetry;

/// <summary>
/// Provides OpenTelemetry tracing instrumentation for execution pipeline stages.
/// Each pipeline stage (ManifestValidator, SignatureVerifier, HashVerifier, CapabilityResolver,
/// PluginLoader, PluginExecutor) gets its own span with execution metadata.
/// Telemetry overhead is designed to stay under 5ms per execution.
/// </summary>
public static class PipelineTelemetry
{
    private static readonly ActivitySource Source = new("PluginRuntime");

    /// <summary>
    /// Starts a new span for a pipeline stage.
    /// Records ExecutionId, PluginId, and Version as span tags.
    /// </summary>
    public static Activity? StartStage(string stageName, string executionId, Guid pluginId, string? version)
    {
        var activity = Source.StartActivity(stageName, ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag("execution.id", executionId);
            activity.SetTag("plugin.id", pluginId.ToString());
            activity.SetTag("plugin.version", version ?? "latest");
        }
        return activity;
    }

    /// <summary>
    /// Ends a pipeline stage span, recording status and optional memory usage.
    /// Duration is automatically captured by the Activity's StartTime/EndTime.
    /// </summary>
    public static void EndStage(Activity? activity, string status, double? memoryUsageMb = null)
    {
        if (activity is null) return;

        activity.SetTag("stage.status", status);
        if (memoryUsageMb.HasValue)
            activity.SetTag("memory.usage.mb", memoryUsageMb.Value);

        if (status != "Success")
            activity.SetStatus(ActivityStatusCode.Error, status);

        activity.Stop();
    }

    /// <summary>
    /// Starts the root execution span that encompasses all pipeline stages.
    /// </summary>
    public static Activity? StartExecution(string executionId, Guid pluginId, string? version)
    {
        var activity = Source.StartActivity("PluginExecution", ActivityKind.Server);
        if (activity is not null)
        {
            activity.SetTag("execution.id", executionId);
            activity.SetTag("plugin.id", pluginId.ToString());
            activity.SetTag("plugin.version", version ?? "latest");
        }
        return activity;
    }

    /// <summary>
    /// Ends the root execution span with final status and duration.
    /// </summary>
    public static void EndExecution(Activity? activity, string status, double? memoryUsageMb = null)
    {
        if (activity is null) return;

        activity.SetTag("execution.status", status);
        if (memoryUsageMb.HasValue)
            activity.SetTag("execution.memory.usage.mb", memoryUsageMb.Value);

        if (status != "Completed")
            activity.SetStatus(ActivityStatusCode.Error, status);

        activity.Stop();
    }
}
