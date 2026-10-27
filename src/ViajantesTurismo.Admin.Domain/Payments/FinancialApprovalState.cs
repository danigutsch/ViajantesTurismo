namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted explicit approval of a financial action.</summary>
/// <param name="ActorId">Identifier of the approving actor.</param>
/// <param name="ApprovedAt">UTC approval instant.</param>
/// <param name="Reason">Reason code of the approval.</param>
internal sealed record FinancialApprovalState(string ActorId, DateTimeOffset ApprovedAt, FinancialReason Reason)
{
    public override string ToString() => "FinancialApprovalState [redacted]";
}
