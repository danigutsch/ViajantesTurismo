using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Bookings;

namespace ViajantesTurismo.Admin.UnitTests.Application.Bookings;

/// <summary>
/// Booking-scoped command returning a value result, so guard behaviors are exercised with <c>Result&lt;T&gt;</c>.
/// </summary>
/// <param name="BookingId">The targeted booking identifier.</param>
internal sealed record StubBookingValueCommand(Guid BookingId) : ICommand<Result<Guid>>, IBookingScopedCommand;
