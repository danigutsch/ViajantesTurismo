namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted snapshot of a booking's append-only financial history.</summary>
/// <param name="SchemaVersion">Version of the stored snapshot layout; currently 1.</param>
/// <param name="BookingId">Identifier of the owning booking.</param>
/// <param name="Version">Optimistic concurrency version, incremented on every change.</param>
/// <param name="LastChangedAt">UTC instant of the latest change.</param>
/// <param name="Schedules">Drafted and approved payment schedule revisions, oldest first.</param>
/// <param name="Quotes">Drafted and approved agreed FX quotes.</param>
/// <param name="Receipts">Recorded payment receipts.</param>
/// <param name="Reconciliations">Receipt reconciliations that turn receipts into EUR credit.</param>
/// <param name="Allocations">EUR credit allocated from receipts to installments.</param>
/// <param name="Releases">Approved releases of earlier allocations.</param>
/// <param name="Refunds">Drafted and approved refunds.</param>
/// <param name="Reversals">Approved receipt reversals.</param>
/// <param name="RefundCorrections">Approved corrections of refunds.</param>
/// <param name="Reviews">Review decisions recorded against drafts.</param>
/// <param name="Withdrawals">Withdrawals of previously granted approvals.</param>
internal sealed record BookingFinancialState(int SchemaVersion, Guid BookingId, long Version, DateTimeOffset LastChangedAt, PaymentScheduleState[] Schedules, BookingQuoteState[] Quotes, PaymentReceiptState[] Receipts, ReceiptReconciliationState[] Reconciliations, PaymentAllocationState[] Allocations, PaymentAllocationReleaseState[] Releases, PaymentRefundState[] Refunds, PaymentReceiptReversalState[] Reversals, PaymentRefundCorrectionState[] RefundCorrections, FinancialReviewState[] Reviews, FinancialApprovalWithdrawalState[] Withdrawals)
{
    public override string ToString() => "BookingFinancialState [redacted]";
}
