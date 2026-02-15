using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Bookings.RecordPayment;

/// <summary>
/// Handles recording a payment for a booking.
/// </summary>
public sealed class RecordPaymentCommandHandler(
    ITourStore tourStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Handles the RecordPaymentCommand and returns the payment ID.
    /// </summary>
    /// <param name="command">The command containing the payment data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the payment ID if successful, or validation errors.</returns>
    public async Task<Result<Guid>> Handle(RecordPaymentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tour = await tourStore.GetByBookingId(command.BookingId, ct);
        if (tour is null)
        {
            return BookingErrors.BookingNotFound(command.BookingId).ConvertError<Guid>();
        }

        var result = tour.RecordBookingPayment(
            command.BookingId,
            command.Amount,
            command.PaymentDate,
            BookingMapper.MapToPaymentMethod(command.Method),
            timeProvider,
            command.ReferenceNumber,
            command.Notes);

        if (result.IsFailure)
        {
            return result.ConvertError<Payment, Guid>();
        }

        await unitOfWork.SaveEntities(ct);

        return Result<Guid>.Ok(result.Value.Id);
    }
}
