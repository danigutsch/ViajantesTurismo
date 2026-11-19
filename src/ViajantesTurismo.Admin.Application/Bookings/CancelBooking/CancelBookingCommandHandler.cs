using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Bookings.CancelBooking;

/// <summary>
/// Handles the cancellation of a booking.
/// </summary>
public sealed class CancelBookingCommandHandler(
    ITourCommandStore tourStore,
    IUnitOfWork unitOfWork) : ICommandHandler<CancelBookingCommand, Result>
{
    /// <summary>
    /// Handles the CancelBookingCommand and returns the result.
    /// </summary>
    /// <param name="command">The command containing the booking ID to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async ValueTask<Result> Handle(CancelBookingCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var storedTour = await tourStore.GetByBookingId(command.BookingId, ct);
        if (!storedTour.TryGetValue(out var tour))
        {
            return BookingErrors.BookingNotFound();
        }

        var result = tour.CancelBooking(command.BookingId);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
