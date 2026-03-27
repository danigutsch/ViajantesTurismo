using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Bookings.CreateBooking;

/// <summary>
/// Handles the creation of a new booking with application-level validation.
/// </summary>
public sealed class CreateBookingCommandHandler(
    ITourStore tourStore,
    ICustomerStore customerStore,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles the CreateBookingCommand and returns the ID of the created booking.
    /// </summary>
    /// <param name="command">The command containing booking data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the booking ID if successful, or validation errors.</returns>
    public async Task<Result<Guid>> Handle(CreateBookingCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tour = await tourStore.GetById(command.TourId, ct);
        if (tour is null)
        {
            return TourErrors.TourNotFound(command.TourId).ConvertError<Tour, Guid>();
        }

        var principalCustomer = await customerStore.GetById(command.PrincipalCustomerId, ct);
        if (principalCustomer is null)
        {
            return CustomerErrors.CustomerNotFound(command.PrincipalCustomerId).ConvertError<Guid>();
        }

        if (command.CompanionCustomerId.HasValue)
        {
            var companionCustomer = await customerStore.GetById(command.CompanionCustomerId.Value, ct);
            if (companionCustomer is null)
            {
                return CustomerErrors.CustomerNotFound(command.CompanionCustomerId.Value).ConvertError<Guid>();
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
            command.Notes));

        if (!result.IsSuccess)
        {
            return result.ConvertError<Booking, Guid>();
        }

        await unitOfWork.SaveEntities(ct);

        return result.Value.Id;
    }
}
