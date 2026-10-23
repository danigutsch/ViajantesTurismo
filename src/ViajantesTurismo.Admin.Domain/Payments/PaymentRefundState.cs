namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentRefundState(Guid Id, Guid ReceiptId, decimal OriginalCashBasis, decimal EuroCreditReversed, decimal EuroCashPayable, decimal ActualSettlement, decimal FxDifference, Guid? QuoteId, PaymentSelectionState[] ReversedAllocations, string Reference, DateTimeOffset PerformedAt, string CreatedBy, DateTimeOffset CreatedAt, FinancialApprovalState? Approval)
{
    public override string ToString() => "PaymentRefundState [redacted]";
}
