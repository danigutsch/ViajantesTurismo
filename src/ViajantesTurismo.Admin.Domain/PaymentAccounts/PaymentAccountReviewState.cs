namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Persistence snapshot of a review decision on a payment account revision.</summary>
/// <param name="Revision">The number of the reviewed revision.</param>
/// <param name="ReviewerId">The opaque identifier of the reviewing Admin.</param>
/// <param name="ReviewedAt">The UTC instant of the review.</param>
/// <param name="Outcome">The review outcome.</param>
internal sealed record PaymentAccountReviewState(
    int Revision, string ReviewerId, DateTimeOffset ReviewedAt, PaymentAccountReviewOutcome Outcome);
