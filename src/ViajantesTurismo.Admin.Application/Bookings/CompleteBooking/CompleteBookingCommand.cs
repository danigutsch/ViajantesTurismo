using PandaBear.Mediator;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Bookings.CompleteBooking;

/// <summary>
/// Command to complete a booking.
/// </summary>
public sealed record CompleteBookingCommand(Guid BookingId) : ICommand<Result>, IBookingScopedCommand;
