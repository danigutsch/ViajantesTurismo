namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted reconciliation of a receipt into EUR credit.</summary>
/// <param name="Id">Reconciliation identifier.</param>
/// <param name="ReceiptId">Identifier of the reconciled receipt.</param>
/// <param name="EuroCredit">EUR credit the receipt is worth.</param>
/// <param name="QuoteId">Identifier of the quote used; null for EUR receipts.</param>
/// <param name="InstructionId">Identifier of the matched payment instruction, if any.</param>
/// <param name="Allocations">Proposed allocations of the credit.</param>
/// <param name="CreatedBy">Actor who drafted the reconciliation.</param>
/// <param name="CreatedAt">UTC draft instant.</param>
/// <param name="Approval">Approval; null while it is a draft.</param>
internal sealed record ReceiptReconciliationState(Guid Id, Guid ReceiptId, decimal EuroCredit, Guid? QuoteId, Guid? InstructionId, PaymentSelectionState[] Allocations, string CreatedBy, DateTimeOffset CreatedAt, FinancialApprovalState? Approval)
{
    public override string ToString() => "ReceiptReconciliationState [redacted]";
}
