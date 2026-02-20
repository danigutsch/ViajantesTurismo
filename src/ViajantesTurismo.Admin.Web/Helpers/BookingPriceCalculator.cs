using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Web.Helpers;

/// <summary>
/// Helper class for calculating booking prices.
/// </summary>
public static class BookingPriceCalculator
{
    public static decimal CalculateSubtotal(
        RoomTypeDto roomType,
        BikeTypeDto principalBikeType,
        BikeTypeDto? companionBikeType,
        decimal basePrice,
        decimal singleRoomSupplement,
        decimal regularBikePrice,
        decimal eBikePrice)
    {
        var roomCost = roomType == RoomTypeDto.SingleOccupancy
            ? singleRoomSupplement
            : 0m;

        var principalBikeCost = principalBikeType switch
        {
            BikeTypeDto.Regular => regularBikePrice,
            BikeTypeDto.EBike => eBikePrice,
            _ => 0m
        };

        var companionBikeCost = companionBikeType switch
        {
            BikeTypeDto.Regular => regularBikePrice,
            BikeTypeDto.EBike => eBikePrice,
            _ => 0m
        };

        return basePrice + roomCost + principalBikeCost + companionBikeCost;
    }

    public static decimal CalculateDiscountAmount(
        DiscountTypeDto discountType,
        decimal discountAmount,
        decimal subtotal)
    {
        if (discountType == DiscountTypeDto.None || discountAmount <= 0)
        {
            return 0m;
        }

        return discountType switch
        {
            DiscountTypeDto.Percentage => subtotal * (discountAmount / 100m),
            DiscountTypeDto.Absolute => discountAmount,
            _ => 0m
        };
    }

    public static decimal CalculateFinalTotal(decimal subtotal, decimal discountAmount)
    {
        return Math.Max(0, subtotal - discountAmount);
    }
}
