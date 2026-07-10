using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;

/// <summary>
/// Command to update a booking's details.
/// </summary>
public sealed record UpdateBookingDetailsCommand(
    Guid BookingId,
    RoomTypeDto RoomType,
    BikeTypeDto PrincipalBikeType,
    Guid? CompanionCustomerId,
    BikeTypeDto? CompanionBikeType);
