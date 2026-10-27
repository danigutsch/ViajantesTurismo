namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted approved reversal of a receipt.</summary>
/// <param name="Id">Reversal identifier.</param>
/// <param name="ReceiptId">Identifier of the reversed receipt.</param>
/// <param name="AllocationIds">Identifiers of the allocations undone.</param>
/// <param name="Approval">Approval of the reversal.</param>
internal sealed record PaymentReceiptReversalState(Guid Id, Guid ReceiptId, Guid[] AllocationIds, FinancialApprovalState Approval)
{
    public override string ToString() => "PaymentReceiptReversalState [redacted]";
}
