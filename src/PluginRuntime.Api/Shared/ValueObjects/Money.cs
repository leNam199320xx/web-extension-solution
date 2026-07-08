using PluginRuntime.Api.Shared.Exceptions;

namespace PluginRuntime.Api.Shared.ValueObjects;

/// <summary>
/// Money value object representing a non-negative monetary amount with currency.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency code cannot be empty.");

        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
