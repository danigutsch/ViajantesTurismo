using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Bookings.CreateBooking;

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
    string? Notes) : ICommand<Result<Guid>>
{
    /// <summary>Gets the booking identifier retained across attempts of this command.</summary>
    public Guid BookingId { get; } = Guid.CreateVersion7();
}
