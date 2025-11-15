using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Bookings.Commands.ConfirmBooking;

/// <summary>
/// Handles the confirmation of a booking.
/// </summary>
public sealed class ConfirmBookingCommandHandler(
    ITourStore tourStore,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles the ConfirmBookingCommand and returns the result.
    /// </summary>
    /// <param name="command">The command containing the booking ID to confirm.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> Handle(ConfirmBookingCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tour = await tourStore.GetByBookingId(command.BookingId, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(command.BookingId);
        }

        var result = tour.ConfirmBooking(command.BookingId);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
