using ViajantesTurismo.Admin.Domain.Customers;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Bookings.CreateBooking;

/// <summary>
/// Handles the creation of a new booking with application-level validation.
/// </summary>
public sealed class CreateBookingCommandHandler(
    ITourCommandStore tourStore,
    ICustomerStore customerStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<CreateBookingCommand, Result<Guid>>
{
    /// <summary>
    /// Handles the CreateBookingCommand and returns the ID of the created booking.
    /// </summary>
    /// <param name="command">The command containing booking data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the booking ID if successful, or validation errors.</returns>
    public async ValueTask<Result<Guid>> Handle(CreateBookingCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var storedTour = await tourStore.GetById(command.TourId, ct);
        if (!storedTour.TryGetValue(out var tour))
        {
            return TourErrors.TourNotFound().ConvertError<Guid>();
        }

        if (tour.HasBooking(command.BookingId))
        {
            return Result.Ok(command.BookingId);
        }

        var principalCustomer = await customerStore.GetById(command.PrincipalCustomerId, ct);
        if (principalCustomer is null)
        {
            return CustomerErrors.CustomerNotFound().ConvertError<Guid>();
        }

        if (command.CompanionCustomerId.HasValue)
        {
            var companionCustomer = await customerStore.GetById(command.CompanionCustomerId.Value, ct);
            if (companionCustomer is null)
            {
                return CustomerErrors.CustomerNotFound().ConvertError<Guid>();
            }
        }

        var result = tour.AddBooking(new TourBookingRequest(
            new BookingTravelers(
                command.PrincipalCustomerId,
                BookingMapper.MapToBikeType(command.PrincipalBikeType),
                command.CompanionCustomerId,
                command.CompanionBikeType.HasValue
                    ? BookingMapper.MapToBikeType(command.CompanionBikeType.Value)
                    : null),
            BookingMapper.MapToRoomType(command.RoomType),
            new BookingDiscountDefinition(
                BookingMapper.MapToDiscountType(command.DiscountType),
                command.DiscountAmount,
                command.DiscountReason),
            command.Notes), timeProvider.GetUtcNow().UtcDateTime, command.BookingId);

        if (!result.IsSuccess)
        {
            return result.ConvertError<Booking, Guid>();
        }

        await unitOfWork.SaveEntities(ct);

        return Result.Ok(result.Value.Id);
    }
}
