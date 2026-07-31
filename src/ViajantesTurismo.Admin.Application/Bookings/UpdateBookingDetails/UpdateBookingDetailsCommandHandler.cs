using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Application.Tours;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;

/// <summary>
/// Handles updating a booking's details.
/// </summary>
public sealed class UpdateBookingDetailsCommandHandler(
    ITourStore tourStore,
    ITourCapacityMutationLock capacityMutationLock,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles the UpdateBookingDetailsCommand and returns the result.
    /// </summary>
    /// <param name="command">The command containing the booking details update data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> Handle(UpdateBookingDetailsCommand command, CancellationToken ct)
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

        var result = tour.UpdateBookingDetails(
            command.BookingId,
            BookingMapper.MapToRoomType(command.RoomType),
            BookingMapper.MapToBikeType(command.PrincipalBikeType),
            command.CompanionCustomerId,
            command.CompanionBikeType.HasValue ? BookingMapper.MapToBikeType(command.CompanionBikeType.Value) : null);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
