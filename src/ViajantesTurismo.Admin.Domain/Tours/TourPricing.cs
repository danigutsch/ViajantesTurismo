using PandaBear.Results;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents tour pricing information with validation.
/// </summary>
public sealed class TourPricing : ValueObject
{
    private TourPricing(
        decimal basePrice,
        decimal singleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency)
    {
        BasePrice = basePrice;
        SingleRoomSupplementPrice = singleRoomSupplementPrice;
        RegularBikePrice = regularBikePrice;
        EBikePrice = eBikePrice;
        Currency = currency;
    }

    /// <summary>
    /// Gets the base price of the tour (double occupancy).
    /// </summary>
    public decimal BasePrice
    {
        get;
    }

    /// <summary>
    /// Gets the supplement price for single room occupancy.
    /// </summary>
    public decimal SingleRoomSupplementPrice
    {
        get;
    }

    /// <summary>
    /// Gets the price for regular bike rental.
    /// </summary>
    public decimal RegularBikePrice
    {
        get;
    }

    /// <summary>
    /// Gets the price for e-bike rental.
    /// </summary>
    public decimal EBikePrice
    {
        get;
    }

    /// <summary>
    /// Gets the currency used for all prices.
    /// </summary>
    public Currency Currency
    {
        get;
    }

    /// <summary>
    /// Creates a new tour pricing with validation.
    /// </summary>
    /// <param name="basePrice">The base price of the tour.</param>
    /// <param name="singleRoomSupplementPrice">The supplement price for a single room.</param>
    /// <param name="regularBikePrice">The price for a regular bike.</param>
    /// <param name="eBikePrice">The price for e-bike.</param>
    /// <param name="currency">The currency for all prices.</param>
    /// <returns>A Result containing the TourPricing if valid, or errors if validation fails.</returns>
    public static Result<TourPricing> Create(
        decimal basePrice,
        decimal singleRoomSupplementPrice,
        decimal regularBikePrice,
        decimal eBikePrice,
        Currency currency)
    {
        basePrice = Math.Round(basePrice, 2, MidpointRounding.AwayFromZero);
        singleRoomSupplementPrice = Math.Round(singleRoomSupplementPrice, 2, MidpointRounding.AwayFromZero);
        regularBikePrice = Math.Round(regularBikePrice, 2, MidpointRounding.AwayFromZero);
        eBikePrice = Math.Round(eBikePrice, 2, MidpointRounding.AwayFromZero);

        var errors = new ValidationErrors();

        if (!Enum.IsDefined(currency))
        {
            errors.Add(TourErrors.InvalidCurrency());
        }

        if (basePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Base price"));
        }
        else if (basePrice > AdminDomainLimits.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Base price"));
        }

        if (singleRoomSupplementPrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Single room supplement price"));
        }
        else if (singleRoomSupplementPrice > AdminDomainLimits.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Single room supplement price"));
        }

        if (regularBikePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("Regular bike price"));
        }
        else if (regularBikePrice > AdminDomainLimits.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("Regular bike price"));
        }

        if (eBikePrice <= 0)
        {
            errors.Add(TourErrors.InvalidPrice("E-bike price"));
        }
        else if (eBikePrice > AdminDomainLimits.MaxPrice)
        {
            errors.Add(TourErrors.PriceTooHigh("E-bike price"));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<TourPricing>();
        }

        return Result.Ok(new TourPricing(basePrice, singleRoomSupplementPrice, regularBikePrice, eBikePrice, currency));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BasePrice;
        yield return SingleRoomSupplementPrice;
        yield return RegularBikePrice;
        yield return EBikePrice;
        yield return Currency;
    }
}
