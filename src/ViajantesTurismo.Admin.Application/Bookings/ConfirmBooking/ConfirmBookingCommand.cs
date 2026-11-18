using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Pipeline;

namespace ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;

/// <summary>
/// Command to confirm a booking.
/// </summary>
public sealed record ConfirmBookingCommand(Guid BookingId) : ICommand<Result>, ITransactionalAdminCommand, IBookingScopedCommand;
