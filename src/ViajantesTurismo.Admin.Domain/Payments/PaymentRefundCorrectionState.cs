namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted approved correction of a refund.</summary>
/// <param name="Id">Correction identifier.</param>
/// <param name="RefundId">Identifier of the corrected refund.</param>
/// <param name="Approval">Approval of the correction.</param>
internal sealed record PaymentRefundCorrectionState(Guid Id, Guid RefundId, FinancialApprovalState Approval)
{
    public override string ToString() => "PaymentRefundCorrectionState [redacted]";
}
