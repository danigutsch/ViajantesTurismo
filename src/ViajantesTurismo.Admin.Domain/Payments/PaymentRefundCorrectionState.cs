namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentRefundCorrectionState(Guid Id, Guid RefundId, FinancialApprovalState Approval)
{
    public override string ToString() => "PaymentRefundCorrectionState [redacted]";
}
