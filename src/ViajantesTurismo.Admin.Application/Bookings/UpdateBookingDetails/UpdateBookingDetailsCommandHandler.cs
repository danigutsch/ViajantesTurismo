using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;

/// <summary>
/// Handles updating a booking's details.
/// </summary>
public sealed class UpdateBookingDetailsCommandHandler(
    ITourCommandStore tourStore,
    ICustomerStore customerStore,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateBookingDetailsCommand, Result>
{
    /// <summary>
    /// Handles the UpdateBookingDetailsCommand and returns the result.
    /// </summary>
    /// <param name="command">The command containing the booking details update data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async ValueTask<Result> Handle(UpdateBookingDetailsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var storedTour = await tourStore.GetByBookingId(command.BookingId, ct);
        if (!storedTour.TryGetValue(out var tour))
        {
            return BookingErrors.BookingNotFound();
        }

        if (command.CompanionCustomerId.HasValue)
        {
            var companion = await customerStore.GetById(command.CompanionCustomerId.Value, ct);
            if (companion is null)
            {
                return CustomerErrors.CustomerNotFound();
            }
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
