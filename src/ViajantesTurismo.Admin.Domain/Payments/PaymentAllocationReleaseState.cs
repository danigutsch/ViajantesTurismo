namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentAllocationReleaseState(Guid Id, Guid AllocationId, decimal EuroAmount, FinancialApprovalState Approval)
{
    public override string ToString() => "PaymentAllocationReleaseState [redacted]";
}
