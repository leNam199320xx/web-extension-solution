namespace PluginRuntime.Api.Modules.Billing.Configuration;

/// <summary>
/// Configuration options for Stripe integration.
/// Bound from the "Stripe" configuration section.
/// </summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; } = null!;
    public string WebhookSecret { get; init; } = null!;
    public string FreePriceId { get; init; } = null!;
    public string ProPriceId { get; init; } = null!;
    public string EnterprisePriceId { get; init; } = null!;
}
