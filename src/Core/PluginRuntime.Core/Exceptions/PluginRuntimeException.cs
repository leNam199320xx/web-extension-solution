namespace PluginRuntime.Core.Exceptions;

/// <summary>
/// Base exception for all Plugin Runtime domain errors.
/// </summary>
public abstract class PluginRuntimeException : Exception
{
    public string ErrorCode { get; }
    public string Category { get; }

    protected PluginRuntimeException(string errorCode, string category, string message)
        : base(message)
    {
        ErrorCode = errorCode;
        Category = category;
    }

    protected PluginRuntimeException(string errorCode, string category, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Category = category;
    }
}
