namespace PluginRuntime.Api.Shared.Exceptions;

/// <summary>
/// Thrown when the billing provider (Stripe) encounters an error.
/// The internal detail is logged but never returned to the client.
/// </summary>
public sealed class BillingProviderException : UnifiedApiException
{
    /// <summary>
    /// Internal detail for logging purposes only. Never exposed in API responses.
    /// </summary>
    public string InternalDetail { get; }

    public BillingProviderException(string internalDetail)
        : base("UA-BILL-001", 502, "Billing provider error")
    {
        InternalDetail = internalDetail;
    }
}
