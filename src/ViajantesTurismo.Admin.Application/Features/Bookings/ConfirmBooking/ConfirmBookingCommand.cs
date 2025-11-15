namespace ViajantesTurismo.Admin.Application.Features.Bookings.ConfirmBooking;

/// <summary>
/// Command to confirm a booking.
/// </summary>
public sealed record ConfirmBookingCommand(Guid BookingId);
