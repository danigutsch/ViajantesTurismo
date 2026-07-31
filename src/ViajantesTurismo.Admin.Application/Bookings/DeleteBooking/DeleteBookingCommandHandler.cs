using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;

/// <summary>
/// Handles the deletion of a booking.
/// </summary>
public sealed class DeleteBookingCommandHandler(
    ITourStore tourStore,
    ITourCapacityMutationLock capacityMutationLock,
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

        var tourId = await tourStore.GetTourIdByBookingId(command.BookingId, ct);
        if (!tourId.TryGetValue(out var owningTourId))
        {
            return BookingErrors.BookingNotFound(command.BookingId);
        }

        await using var capacityLease = await capacityMutationLock.Acquire(owningTourId, ct);
        var tour = await tourStore.GetById(owningTourId, ct);
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
