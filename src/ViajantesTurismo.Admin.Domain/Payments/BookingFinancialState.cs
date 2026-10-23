namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record BookingFinancialState(int SchemaVersion, Guid BookingId, long Version, DateTimeOffset LastChangedAt, PaymentScheduleState[] Schedules, BookingQuoteState[] Quotes, PaymentReceiptState[] Receipts, ReceiptReconciliationState[] Reconciliations, PaymentAllocationState[] Allocations, PaymentAllocationReleaseState[] Releases, PaymentRefundState[] Refunds, PaymentReceiptReversalState[] Reversals, PaymentRefundCorrectionState[] RefundCorrections, FinancialReviewState[] Reviews, FinancialApprovalWithdrawalState[] Withdrawals)
{
    public override string ToString() => "BookingFinancialState [redacted]";
}
