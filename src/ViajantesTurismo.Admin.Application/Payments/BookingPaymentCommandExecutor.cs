using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Loads the owning Tour inside a financial transaction and stages only its changed booking history.</summary>
public sealed class BookingPaymentCommandExecutor(ITourStore tours, IPaymentHistoryStore histories, PaymentCommandExecutor commands)
{
    /// <summary>Executes one booking financial operation under a stable resource lock and durable idempotency ownership.</summary>
    public Task<Result<Guid>> Execute(Guid bookingId, string operation, string? key, string fingerprint, Func<Tour, Task<Result<Guid>>> mutate, CancellationToken ct) =>
        commands.Execute("booking-payment:" + operation + ":" + bookingId.ToString("N"), key, fingerprint, ["admin:booking-finance:" + bookingId.ToString("N")], async () =>
        {
            var tour = await tours.GetByBookingId(bookingId, ct);
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
