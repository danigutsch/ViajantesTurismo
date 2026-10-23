namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record ReceiptReconciliationState(Guid Id, Guid ReceiptId, decimal EuroCredit, Guid? QuoteId, Guid? InstructionId, PaymentSelectionState[] Allocations, string CreatedBy, DateTimeOffset CreatedAt, FinancialApprovalState? Approval)
{
    public override string ToString() => "ReceiptReconciliationState [redacted]";
}
