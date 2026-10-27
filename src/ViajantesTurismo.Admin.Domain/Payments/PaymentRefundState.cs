namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted refund of a receipt.</summary>
/// <param name="Id">Refund identifier.</param>
/// <param name="ReceiptId">Identifier of the refunded receipt.</param>
/// <param name="OriginalCashBasis">Original cash amount the refund is based on.</param>
/// <param name="EuroCreditReversed">EUR credit reversed by the refund.</param>
/// <param name="EuroCashPayable">EUR cash payable to the customer.</param>
/// <param name="ActualSettlement">Actual amount settled in the refund currency.</param>
/// <param name="FxDifference">Exchange difference between the original and refund amounts.</param>
/// <param name="QuoteId">Identifier of the refund quote; null when none applies.</param>
/// <param name="ReversedAllocations">Allocations reversed by the refund.</param>
/// <param name="Reference">Refund reference.</param>
/// <param name="PerformedAt">UTC instant the refund was performed.</param>
/// <param name="CreatedBy">Actor who drafted the refund.</param>
/// <param name="CreatedAt">UTC draft instant.</param>
/// <param name="Approval">Approval of the refund; null while it is a draft.</param>
internal sealed record PaymentRefundState(Guid Id, Guid ReceiptId, decimal OriginalCashBasis, decimal EuroCreditReversed, decimal EuroCashPayable, decimal ActualSettlement, decimal FxDifference, Guid? QuoteId, PaymentSelectionState[] ReversedAllocations, string Reference, DateTimeOffset PerformedAt, string CreatedBy, DateTimeOffset CreatedAt, FinancialApprovalState? Approval)
{
    public override string ToString() => "PaymentRefundState [redacted]";
}
