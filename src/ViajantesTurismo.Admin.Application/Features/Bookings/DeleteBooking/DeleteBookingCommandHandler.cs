using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Features.Bookings.DeleteBooking;

/// <summary>
/// Handles the deletion of a booking.
/// </summary>
public sealed class DeleteBookingCommandHandler(
    ITourStore tourStore,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles the DeleteBookingCommand and returns the result.
    /// </summary>
    /// <param name="command">The command containing the booking ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> Handle(DeleteBookingCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tour = await tourStore.GetByBookingId(command.BookingId, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(command.BookingId);
        }

        var result = tour.RemoveBooking(command.BookingId);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
