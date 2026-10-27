namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted release of EUR credit from an allocation.</summary>
/// <param name="Id">Release identifier.</param>
/// <param name="AllocationId">Identifier of the released allocation.</param>
/// <param name="EuroAmount">EUR amount released.</param>
/// <param name="Approval">Approval of the release.</param>
internal sealed record PaymentAllocationReleaseState(Guid Id, Guid AllocationId, decimal EuroAmount, FinancialApprovalState Approval)
{
    public override string ToString() => "PaymentAllocationReleaseState [redacted]";
}
