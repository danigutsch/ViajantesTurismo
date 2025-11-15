using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDiscount;

/// <summary>
/// Command to update a booking's discount.
/// </summary>
public sealed record UpdateBookingDiscountCommand(
    Guid BookingId,
    DiscountTypeDto DiscountType,
    decimal DiscountAmount,
    string? DiscountReason);
