namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted review decision on a draft.</summary>
/// <param name="Id">Review identifier.</param>
/// <param name="TargetId">Identifier of the reviewed draft.</param>
/// <param name="TargetRevision">Revision number of the reviewed draft.</param>
/// <param name="ReviewerId">Identifier of the reviewing actor.</param>
/// <param name="ReviewedAt">UTC review instant.</param>
/// <param name="ChangesRequested">Whether the reviewer requested changes rather than accepting.</param>
internal sealed record FinancialReviewState(Guid Id, Guid TargetId, int TargetRevision, string ReviewerId, DateTimeOffset ReviewedAt, bool ChangesRequested)
{
    public override string ToString() => "FinancialReviewState [redacted]";
}
