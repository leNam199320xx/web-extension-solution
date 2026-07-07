namespace PluginRuntime.Core.Exceptions;

/// <summary>
/// Thrown when a required infrastructure service (PostgreSQL, Redis, or object storage)
/// is unreachable. The system fails closed — no fallback to degraded mode.
/// </summary>
public class InfrastructureUnavailableException : PluginRuntimeException
{
    public string ServiceName { get; }

    public InfrastructureUnavailableException(string serviceName, string message)
        : base(
            "INFRASTRUCTURE_UNAVAILABLE",
            "Execution",
            message)
    {
        ServiceName = serviceName;
    }

    public InfrastructureUnavailableException(string serviceName, string message, Exception innerException)
        : base(
            "INFRASTRUCTURE_UNAVAILABLE",
            "Execution",
            message,
            innerException)
    {
        ServiceName = serviceName;
    }
}
