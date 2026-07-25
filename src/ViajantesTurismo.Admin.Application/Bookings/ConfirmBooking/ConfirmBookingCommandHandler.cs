using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;

/// <summary>
/// Handles the confirmation of a booking.
/// </summary>
public sealed class ConfirmBookingCommandHandler(
    ITourStore tourStore,
    ITourCapacityMutationLock capacityMutationLock,
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

        var tourId = await tourStore.GetTourIdByBookingId(command.BookingId, ct);
        if (tourId is not { } owningTourId)
        {
            return BookingErrors.BookingNotFound(command.BookingId);
        }

        await using var capacityLease = await capacityMutationLock.Acquire(owningTourId, ct);
        var tour = await tourStore.GetById(owningTourId, ct);
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
