namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record FinancialReviewState(Guid Id, Guid TargetId, int TargetRevision, string ReviewerId, DateTimeOffset ReviewedAt, bool ChangesRequested)
{
    public override string ToString() => "FinancialReviewState [redacted]";
}
