using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Management.Web.Components.Shared;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>An immutable display snapshot of an incomplete booking form, before server-side domain validation.</summary>
/// <remarks>The preview keeps the existing zero clamp and tolerates unselected options. The server owns booking acceptance.</remarks>
internal readonly record struct BookingPricePreview
{
    private BookingPricePreview(decimal subtotal, decimal discountAmount)
    {
        Subtotal = subtotal;
        DiscountAmount = discountAmount;
    }

    /// <summary>Gets the selected tour, room and bike subtotal.</summary>
    public decimal Subtotal
    {
        get;
    }

    /// <summary>Gets the proposed discount amount.</summary>
    public decimal DiscountAmount
    {
        get;
    }

    /// <summary>Gets the proposed final total, clamped to zero for display.</summary>
    public decimal TotalPrice => Math.Max(0, Subtotal - DiscountAmount);

    /// <summary>Captures selected prices and discount inputs without retaining the mutable form model.</summary>
    public static BookingPricePreview Create(GetTourDto tour, BookingFormModel selection)
    {
        ArgumentNullException.ThrowIfNull(tour);
        ArgumentNullException.ThrowIfNull(selection);

        var roomCost = selection.RoomType == RoomTypeDto.SingleOccupancy ? tour.SingleRoomSupplementPrice : 0m;
        var subtotal = tour.Price + roomCost
            + GetBikePrice(tour, selection.PrincipalBikeType)
            + GetBikePrice(tour, selection.CompanionBikeType);
        var discountAmount = selection.DiscountAmount <= 0 ? 0m : selection.DiscountType switch
        {
            DiscountTypeDto.Percentage => subtotal * (selection.DiscountAmount / 100m),
            DiscountTypeDto.Absolute => selection.DiscountAmount,
            _ => 0m
        };

        return new BookingPricePreview(subtotal, discountAmount);
    }

    private static decimal GetBikePrice(GetTourDto tour, BikeTypeDto? bikeType) => bikeType switch
    {
        BikeTypeDto.Regular => tour.RegularBikePrice,
        BikeTypeDto.EBike => tour.EBikePrice,
        _ => 0m
    };
}
