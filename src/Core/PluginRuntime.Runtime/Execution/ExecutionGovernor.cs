using System.Diagnostics;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;

namespace PluginRuntime.Runtime.Execution;

/// <summary>
/// Enforces resource limits (timeout, memory, CPU) on plugin execution
/// using CancellationToken as the cooperative enforcement mechanism.
/// Plugins must observe the provided CancellationToken cooperatively.
/// </summary>
public class ExecutionGovernor : IExecutionGovernor
{
    private static readonly TimeSpan MemoryCheckInterval = TimeSpan.FromMilliseconds(100);

    public async Task<T> ExecuteWithLimitsAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ResourceLimits limits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(limits);

        // Create a linked CancellationTokenSource combining:
        // 1. External cancellation (caller's token)
        // 2. Timeout-based cancellation (cancels token when TimeoutMs expires)
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(limits.TimeoutMs));

        // Resource monitoring CTS — linked to timeout so it also stops when timeout fires
        using var resourceCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token);

        // Start memory/CPU monitoring task
        var monitorTask = MonitorResourcesAsync(resourceCts, limits, timeoutCts.Token);

        try
        {
            // Execute the action with the resource-limited token
            var result = await action(resourceCts.Token);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // External cancellation — re-throw as-is
            throw;
        }
        catch (OperationCanceledException) when (resourceCts.IsCancellationRequested && !timeoutCts.IsCancellationRequested)
        {
            // Resource limit exceeded (memory or CPU) — resourceCts was cancelled independently
            throw new InvalidOperationException(
                $"Plugin execution exceeded resource limits (Memory: {limits.MaxMemoryMb}MB, CPU: {limits.MaxCpuMs}ms).");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Timeout exceeded
            throw new TimeoutException(
                $"Plugin execution exceeded timeout of {limits.TimeoutMs}ms.");
        }
        finally
        {
            // Stop monitoring
            await resourceCts.CancelAsync();
            try
            {
                await monitorTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when execution completes or is cancelled
            }
        }
    }

    private static async Task MonitorResourcesAsync(
        CancellationTokenSource resourceCts,
        ResourceLimits limits,
        CancellationToken stopToken)
    {
        var startMemory = GC.GetTotalMemory(forceFullCollection: false);
        var cpuStopwatch = Stopwatch.StartNew();

        try
        {
            while (!stopToken.IsCancellationRequested)
            {
                await Task.Delay(MemoryCheckInterval, stopToken);

                // Check memory usage against MaxMemoryMb
                var currentMemory = GC.GetTotalMemory(forceFullCollection: false);
                var usedMb = (currentMemory - startMemory) / (1024.0 * 1024.0);
                if (usedMb > limits.MaxMemoryMb)
                {
                    await resourceCts.CancelAsync();
                    return;
                }

                // Check CPU time (cooperative approximation using elapsed wall-clock time)
                if (cpuStopwatch.ElapsedMilliseconds > limits.MaxCpuMs)
                {
                    await resourceCts.CancelAsync();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when execution completes or is cancelled
        }
    }
}
