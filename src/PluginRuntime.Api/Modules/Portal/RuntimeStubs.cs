using System.Text.Json;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Api.Modules.Portal;

/// <summary>
/// Demo-mode rate limiter — always allows requests.
/// Replace with Redis-based sliding window in production.
/// </summary>
internal sealed class DemoRateLimiter : IRateLimiter
{
    public Task<RateLimitResult> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken)
    {
        return Task.FromResult(new RateLimitResult(IsAllowed: true, Remaining: maxRequests - 1, RetryAfter: TimeSpan.Zero));
    }
}

/// <summary>
/// Demo-mode execution pipeline — returns a mock successful execution.
/// Replace with real ExecutionPipeline (from PluginRuntime.Runtime) when full infrastructure is wired.
/// </summary>
internal sealed class DemoExecutionPipeline : IExecutionPipeline
{
    public Task<ExecutionResult> ProcessAsync(ExecutionRequest request, CancellationToken cancellationToken)
    {
        var executionId = Guid.NewGuid().ToString("N");
        var traceId = Guid.NewGuid().ToString("N");

        // Simulate a successful execution with sample output
        var output = JsonSerializer.SerializeToElement(new
        {
            message = $"Plugin {request.PluginId} executed successfully (demo mode).",
            input_received = request.Input,
            timestamp = DateTime.UtcNow
        });

        var result = new ExecutionResult(
            Success: true,
            Data: output,
            ExecutionId: executionId,
            TraceId: traceId,
            DurationMs: Random.Shared.Next(20, 200));

        return Task.FromResult(result);
    }
}
