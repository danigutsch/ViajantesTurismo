using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests ApproveBookingPaymentRefund with authenticated actor metadata and stable operation ownership.</summary>
public sealed record ApproveBookingPaymentRefundCommand(Guid BookingId, Guid RefundId, ApproveFinancialRecordDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ApproveBookingPaymentRefundCommand [redacted]";
}
