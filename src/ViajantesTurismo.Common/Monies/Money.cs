using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Common.Monies;

/// <summary>
/// Represents a monetary value with an associated currency.
/// </summary>
/// <remarks>
/// The <paramref name="amount"/> should be a non-negative value.
/// </remarks>
public sealed class Money(decimal amount, Currency currency) : ValueObject
{
    /// <summary>
    /// The monetary amount.
    /// </summary>
    public decimal Amount { get; } = amount;

    /// <summary>
    /// The currency of the monetary amount.
    /// </summary>
    public Currency Currency { get; } = currency;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Currency;
        yield return Amount;
    }
}
