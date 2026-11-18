using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Pipeline;

namespace ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;

/// <summary>
/// Command to delete a booking.
/// </summary>
public sealed record DeleteBookingCommand(Guid BookingId) : ICommand<Result>, ITransactionalAdminCommand, IBookingScopedCommand;
