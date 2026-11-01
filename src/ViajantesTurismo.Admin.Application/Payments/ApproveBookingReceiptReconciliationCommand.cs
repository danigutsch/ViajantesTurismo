using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests ApproveBookingReceiptReconciliation with authenticated actor metadata and stable operation ownership.</summary>
public sealed record ApproveBookingReceiptReconciliationCommand(Guid BookingId, Guid ReconciliationId, ApproveFinancialRecordDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ApproveBookingReceiptReconciliationCommand [redacted]";
}
