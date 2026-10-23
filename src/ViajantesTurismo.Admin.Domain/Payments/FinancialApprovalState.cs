namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record FinancialApprovalState(string ActorId, DateTimeOffset ApprovedAt, FinancialReason Reason)
{
    public override string ToString() => "FinancialApprovalState [redacted]";
}
