using PandaBear.Mediator;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Bookings.CancelBooking;

/// <summary>
/// Command to cancel a booking.
/// </summary>
public sealed record CancelBookingCommand(Guid BookingId) : ICommand<Result>, IBookingScopedCommand;
