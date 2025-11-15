namespace ViajantesTurismo.Admin.Application.Features.Bookings.DeleteBooking;

/// <summary>
/// Command to delete a booking.
/// </summary>
public sealed record DeleteBookingCommand(Guid BookingId);
