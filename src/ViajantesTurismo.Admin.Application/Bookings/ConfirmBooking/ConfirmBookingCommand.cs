namespace ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;

/// <summary>
/// Command to confirm a booking.
/// </summary>
public sealed record ConfirmBookingCommand(Guid BookingId);
