using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests ReleaseBookingPaymentAllocations with authenticated actor metadata and stable operation ownership.</summary>
public sealed record ReleaseBookingPaymentAllocationsCommand(Guid BookingId, AllocatePaymentCreditDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ReleaseBookingPaymentAllocationsCommand [redacted]";
}
