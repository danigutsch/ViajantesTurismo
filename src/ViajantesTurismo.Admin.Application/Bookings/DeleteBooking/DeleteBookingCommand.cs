namespace ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;

/// <summary>
/// Command to delete a booking.
/// </summary>
public sealed record DeleteBookingCommand(Guid BookingId);
