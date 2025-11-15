using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Application.Features.Bookings.CreateBooking;

/// <summary>
/// Command to create a new booking.
/// </summary>
public sealed record CreateBookingCommand(
    Guid TourId,
    Guid PrincipalCustomerId,
    BikeTypeDto PrincipalBikeType,
    Guid? CompanionCustomerId,
    BikeTypeDto? CompanionBikeType,
    RoomTypeDto RoomType,
    DiscountTypeDto DiscountType,
    decimal DiscountAmount,
    string? DiscountReason,
    string? Notes);
