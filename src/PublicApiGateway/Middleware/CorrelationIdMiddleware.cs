namespace PublicApiGateway.Middleware;

/// <summary>
/// Validates/generates Correlation ID.
/// - Valid: 1-128 chars, printable ASCII (codes 33-126)
/// - Invalid/missing: generate UUID v4
/// - Stored in HttpContext.Items["CorrelationId"]
/// - Always returned in X-Correlation-Id response header
/// </summary>
public sealed class CorrelationIdMiddleware : IMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private const string ContextKey = "CorrelationId";
    private const int MaxLength = 128;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = ExtractOrGenerate(context);

        context.Items[ContextKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }

    private static string ExtractOrGenerate(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
            return Guid.NewGuid().ToString();

        var value = headerValue.ToString();

        if (string.IsNullOrEmpty(value) || value.Length > MaxLength || !IsValidCorrelationId(value))
            return Guid.NewGuid().ToString();

        return value;
    }

    private static bool IsValidCorrelationId(string value)
    {
        foreach (var c in value)
        {
            // Printable ASCII: codes 33–126
            if (c < 33 || c > 126)
                return false;
        }
        return true;
    }
}
