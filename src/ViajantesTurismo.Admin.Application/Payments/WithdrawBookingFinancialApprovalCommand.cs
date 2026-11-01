using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests WithdrawBookingFinancialApproval with authenticated actor metadata and stable operation ownership.</summary>
public sealed record WithdrawBookingFinancialApprovalCommand(Guid BookingId, Guid TargetId, ApproveFinancialRecordDto Input, string ActorId, IdempotencyKey IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "WithdrawBookingFinancialApprovalCommand [redacted]";
}
