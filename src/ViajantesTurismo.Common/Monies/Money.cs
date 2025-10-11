namespace ViajantesTurismo.Common.Monies;

/// <summary>
/// Represents a monetary value with an associated currency.
/// </summary>
/// <remarks>
/// The <paramref name="amount"/> should be a non-negative value.
/// </remarks>
public class Money(decimal amount, Currency currency)
{
    /// <summary>
    /// The monetary amount.
    /// </summary>
    public decimal Amount { get; } = amount;

    /// <summary>
    /// The currency of the monetary amount.
    /// </summary>
    public Currency Currency { get; } = currency;
}