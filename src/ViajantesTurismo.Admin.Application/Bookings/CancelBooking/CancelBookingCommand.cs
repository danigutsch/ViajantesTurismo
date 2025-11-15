namespace ViajantesTurismo.Admin.Application.Bookings.CancelBooking;

/// <summary>
/// Command to cancel a booking.
/// </summary>
public sealed record CancelBookingCommand(Guid BookingId);
