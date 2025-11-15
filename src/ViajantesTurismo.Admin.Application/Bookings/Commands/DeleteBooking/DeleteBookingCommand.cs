namespace ViajantesTurismo.Admin.Application.Bookings.Commands.DeleteBooking;

/// <summary>
/// Command to delete a booking.
/// </summary>
public sealed record DeleteBookingCommand(Guid BookingId);
