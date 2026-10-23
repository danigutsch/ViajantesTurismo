namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentReceiptReversalState(Guid Id, Guid ReceiptId, Guid[] AllocationIds, FinancialApprovalState Approval)
{
    public override string ToString() => "PaymentReceiptReversalState [redacted]";
}
