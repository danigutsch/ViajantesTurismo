using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests AllocateBookingPayment with authenticated actor metadata and stable operation ownership.</summary>
public sealed record AllocateBookingPaymentCommand(Guid BookingId, Guid ReceiptId, AllocatePaymentCreditDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "AllocateBookingPaymentCommand [redacted]";
}
