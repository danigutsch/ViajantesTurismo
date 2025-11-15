namespace ViajantesTurismo.Admin.Application.Bookings.CompleteBooking;

/// <summary>
/// Command to complete a booking.
/// </summary>
public sealed record CompleteBookingCommand(Guid BookingId);
