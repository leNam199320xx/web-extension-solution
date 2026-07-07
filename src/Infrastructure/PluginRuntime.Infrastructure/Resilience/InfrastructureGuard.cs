using Microsoft.Extensions.Logging;
using PluginRuntime.Core.Exceptions;

namespace PluginRuntime.Infrastructure.Resilience;

/// <summary>
/// Wraps infrastructure calls with a 5-second connection timeout and fail-closed semantics.
/// If PostgreSQL, Redis, or object storage is unreachable, logs the failure and throws
/// <see cref="InfrastructureUnavailableException"/>.
/// </summary>
public sealed class InfrastructureGuard
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<InfrastructureGuard> _logger;

    public InfrastructureGuard(ILogger<InfrastructureGuard> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes an infrastructure operation with a 5-second timeout guard.
    /// </summary>
    /// <typeparam name="T">The return type of the infrastructure operation.</typeparam>
    /// <param name="serviceName">Name of the infrastructure service (e.g., "PostgreSQL", "Redis", "ObjectStorage").</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="cancellationToken">Caller-provided cancellation token.</param>
    /// <returns>The result of the infrastructure operation.</returns>
    /// <exception cref="InfrastructureUnavailableException">
    /// Thrown when the operation times out or the service is unreachable.
    /// </exception>
    public async Task<T> ExecuteWithGuardAsync<T>(
        string serviceName,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ConnectionTimeout);

        try
        {
            return await operation(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Infrastructure service '{ServiceName}' connection timed out after {TimeoutSeconds}s.",
                serviceName,
                ConnectionTimeout.TotalSeconds);

            throw new InfrastructureUnavailableException(
                serviceName,
                $"Infrastructure service '{serviceName}' is unreachable (connection timed out after {ConnectionTimeout.TotalSeconds}s).");
        }
        catch (OperationCanceledException)
        {
            // Caller cancelled — rethrow without wrapping.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Infrastructure service '{ServiceName}' is unreachable: {ErrorMessage}",
                serviceName,
                ex.Message);

            throw new InfrastructureUnavailableException(
                serviceName,
                $"Infrastructure service '{serviceName}' is unreachable: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Executes an infrastructure operation (void) with a 5-second timeout guard.
    /// </summary>
    /// <param name="serviceName">Name of the infrastructure service.</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="cancellationToken">Caller-provided cancellation token.</param>
    /// <exception cref="InfrastructureUnavailableException">
    /// Thrown when the operation times out or the service is unreachable.
    /// </exception>
    public async Task ExecuteWithGuardAsync(
        string serviceName,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ConnectionTimeout);

        try
        {
            await operation(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Infrastructure service '{ServiceName}' connection timed out after {TimeoutSeconds}s.",
                serviceName,
                ConnectionTimeout.TotalSeconds);

            throw new InfrastructureUnavailableException(
                serviceName,
                $"Infrastructure service '{serviceName}' is unreachable (connection timed out after {ConnectionTimeout.TotalSeconds}s).");
        }
        catch (OperationCanceledException)
        {
            // Caller cancelled — rethrow without wrapping.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Infrastructure service '{ServiceName}' is unreachable: {ErrorMessage}",
                serviceName,
                ex.Message);

            throw new InfrastructureUnavailableException(
                serviceName,
                $"Infrastructure service '{serviceName}' is unreachable: {ex.Message}",
                ex);
        }
    }
}
