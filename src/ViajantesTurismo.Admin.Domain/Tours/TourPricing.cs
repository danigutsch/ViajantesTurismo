using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents tour pricing information with validation.
/// </summary>
public sealed class TourPricing : ValueObject
{
    private TourPricing(
        decimal basePrice,
        decimal doubleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency)
    {
        BasePrice = basePrice;
        DoubleRoomSupplementPrice = doubleRoomSupplementPrice;
        RegularBikePrice = regularBikePrice;
        EBikePrice = eBikePrice;
        Currency = currency;
    }

    /// <summary>
    /// Gets the base price of the tour (single room).
    /// </summary>
    public decimal BasePrice { get; }

    /// <summary>
    /// Gets the supplement price for double room.
    /// </summary>
    public decimal DoubleRoomSupplementPrice { get; }

    /// <summary>
    /// Gets the price for regular bike rental.
    /// </summary>
    public decimal RegularBikePrice { get; }

    /// <summary>
    /// Gets the price for e-bike rental.
    /// </summary>
    public decimal EBikePrice { get; }

    /// <summary>
    /// Gets the currency used for all prices.
    /// </summary>
    public Currency Currency { get; }

    /// <summary>
    /// Creates a new tour pricing with validation.
    /// </summary>
    /// <param name="basePrice">The base price of the tour.</param>
    /// <param name="doubleRoomSupplementPrice">The supplement price for double room.</param>
    /// <param name="regularBikePrice">The price for a regular bike.</param>
    /// <param name="eBikePrice">The price for e-bike.</param>
    /// <param name="currency">The currency for all prices.</param>
    /// <returns>A Result containing the TourPricing if valid, or errors if validation fails.</returns>
    public static Result<TourPricing> Create(
        decimal basePrice,
        decimal doubleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency)
    {
        basePrice = Math.Round(basePrice, 2, MidpointRounding.AwayFromZero);
        doubleRoomSupplementPrice = Math.Round(doubleRoomSupplementPrice, 2, MidpointRounding.AwayFromZero);
        regularBikePrice = Math.Round(regularBikePrice, 2, MidpointRounding.AwayFromZero);
        eBikePrice = Math.Round(eBikePrice, 2, MidpointRounding.AwayFromZero);

        var errors = new ValidationErrors();

        if (basePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Base price", basePrice));
        }
        else if (basePrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Base price", ContractConstants.MaxPrice, basePrice));
        }

        if (doubleRoomSupplementPrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Double room supplement price", doubleRoomSupplementPrice));
        }
        else if (doubleRoomSupplementPrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Double room supplement price", ContractConstants.MaxPrice, doubleRoomSupplementPrice));
        }

        if (regularBikePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Regular bike price", regularBikePrice));
        }
        else if (regularBikePrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Regular bike price", ContractConstants.MaxPrice, regularBikePrice));
        }

        if (eBikePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("E-bike price", eBikePrice));
        }
        else if (eBikePrice > ContractConstants.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("E-bike price", ContractConstants.MaxPrice, eBikePrice));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<TourPricing>();
        }

        return new TourPricing(basePrice, doubleRoomSupplementPrice, regularBikePrice, eBikePrice, currency);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BasePrice;
        yield return DoubleRoomSupplementPrice;
        yield return RegularBikePrice;
        yield return EBikePrice;
        yield return Currency;
    }
}