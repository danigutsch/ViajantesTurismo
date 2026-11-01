using PandaBear.BuildingBlocks;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>A booking price whose components are in range and whose exact discounted total remains positive.</summary>
/// <remarks>Submitted component prices are normalized by their owning boundary. This value never rounds or clamps them again.</remarks>
internal sealed class BookingPriceBreakdown : ValueObject
{
    private BookingPriceBreakdown(decimal subtotal, decimal discountAmount)
    {
        Subtotal = subtotal;
        DiscountAmount = discountAmount;
    }

    /// <summary>Gets the base price, room supplement and both bike prices before discount.</summary>
    public decimal Subtotal
    {
        get;
    }

    /// <summary>Gets the exact amount deducted by the discount.</summary>
    public decimal DiscountAmount
    {
        get;
    }

    /// <summary>Gets the positive exact subtotal less the discount.</summary>
    public decimal TotalPrice => Subtotal - DiscountAmount;

    /// <summary>Validates price components and the discount before constructing a complete price value.</summary>
    public static Result<BookingPriceBreakdown> Create(
        decimal basePrice,
        decimal roomAdditionalCost,
        decimal principalBikePrice,
        decimal companionBikePrice,
        Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        var components = ValidateComponents(basePrice, roomAdditionalCost, principalBikePrice, companionBikePrice);
        if (components.IsFailure)
        {
            return components.ConvertError<BookingPriceBreakdown>();
        }

        var subtotal = CalculateSubtotal(basePrice, roomAdditionalCost, principalBikePrice, companionBikePrice);
        var discountAmount = discount.CalculateDiscountAmount(subtotal);
        if (discount.Type == DiscountType.Absolute && discountAmount > subtotal)
        {
            return DiscountErrors.AbsoluteDiscountExceedsSubtotal().ConvertError<BookingPriceBreakdown>();
        }

        if (subtotal - discountAmount <= 0)
        {
            return DiscountErrors.FinalPriceNotPositive().ConvertError<BookingPriceBreakdown>();
        }

        return Result.Ok(new BookingPriceBreakdown(subtotal, discountAmount));
    }

    /// <summary>Calculates the exact subtotal, including for previously persisted bookings awaiting revision.</summary>
    internal static decimal CalculateSubtotal(decimal basePrice, decimal roomAdditionalCost, decimal principalBikePrice, decimal companionBikePrice) =>
        basePrice + roomAdditionalCost + principalBikePrice + companionBikePrice;

    /// <summary>Collects intrinsic price component failures for aggregate validation before discount acceptance.</summary>
    internal static Result ValidateComponents(decimal basePrice, decimal roomAdditionalCost, decimal principalBikePrice, decimal companionBikePrice)
    {
        var errors = new ValidationErrors();
        if (basePrice <= 0)
        {
            errors.Add(BookingErrors.ZeroOrNegativeBasePrice());
        }
        else if (basePrice > AdminDomainLimits.MaxPrice)
        {
            errors.Add(BookingErrors.BasePriceExceedsMaximum());
        }

        if (roomAdditionalCost < 0)
        {
            errors.Add(BookingErrors.NegativeRoomCost());
        }
        else if (roomAdditionalCost > AdminDomainLimits.MaxPrice)
        {
            errors.Add(BookingErrors.RoomCostExceedsMaximum());
        }

        ValidateBikePrice(errors, principalBikePrice);
        ValidateBikePrice(errors, companionBikePrice);
        return errors.HasErrors ? errors.ToResult() : Result.Ok();
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Subtotal;
        yield return DiscountAmount;
    }

    private static void ValidateBikePrice(ValidationErrors errors, decimal bikePrice)
    {
        if (bikePrice < 0)
        {
            errors.Add(BookingErrors.NegativeBikePrice());
        }
        else if (bikePrice > AdminDomainLimits.MaxPrice)
        {
            errors.Add(BookingErrors.BikePriceExceedsMaximum());
        }
    }
}
