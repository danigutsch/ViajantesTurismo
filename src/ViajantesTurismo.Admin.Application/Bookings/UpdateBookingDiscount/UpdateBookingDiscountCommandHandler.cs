using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDiscount;

/// <summary>
/// Handles updating a booking's discount.
/// </summary>
public sealed class UpdateBookingDiscountCommandHandler(
    ITourStore tourStore,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles the UpdateBookingDiscountCommand and returns the result.
    /// </summary>
    /// <param name="command">The command containing the booking discount update data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> Handle(UpdateBookingDiscountCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tour = await tourStore.GetByBookingId(command.BookingId, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(command.BookingId);
        }

        var result = tour.UpdateBookingDiscount(
            command.BookingId,
            BookingMapper.MapToDiscountType(command.DiscountType),
            command.DiscountAmount,
            command.DiscountReason);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
