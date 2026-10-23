using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests DraftBookingPaymentRefund with authenticated actor metadata and stable operation ownership.</summary>
public sealed record DraftBookingPaymentRefundCommand(Guid BookingId, Guid ReceiptId, CreatePaymentRefundDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "DraftBookingPaymentRefundCommand [redacted]";
}
