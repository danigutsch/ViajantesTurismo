using SharedKernel.Idempotency;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Loads the owning Tour for update and stages only its changed booking history.</summary>
public sealed class BookingPaymentOperations(ITourCommandStore tours, IPaymentHistoryStore histories, PaymentCommandIdempotency idempotency)
{
    /// <summary>Applies a booking financial operation inside the command's transaction.</summary>
    public ValueTask<Result<Guid>> Execute(Guid bookingId, string scope, IdempotencyKey key, string fingerprint, Func<Tour, Task<Result<Guid>>> mutate, CancellationToken ct) =>
        idempotency.Execute(scope, key, fingerprint, async () =>
        {
            var tour = await tours.GetByBookingIdForPaymentUpdate(bookingId, ct);
            if (tour is null)
            {
                return Result.NotFound(detail: "Booking not found.").ConvertError<Guid>();
            }
            var result = await mutate(tour);
            if (result.IsFailure)
            {
                return result;
            }
            await histories.Save(tour, bookingId, ct);
            return result;
        }, ct);
}
