namespace ViajantesTurismo.Admin.Application.Bookings.Commands.ConfirmBooking;

/// <summary>
/// Command to confirm a booking.
/// </summary>
public sealed record ConfirmBookingCommand(Guid BookingId);
