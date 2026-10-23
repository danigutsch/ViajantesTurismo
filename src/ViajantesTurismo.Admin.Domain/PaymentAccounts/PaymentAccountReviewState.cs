namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

internal sealed record PaymentAccountReviewState(
    int Revision, string ReviewerId, DateTimeOffset ReviewedAt, PaymentAccountReviewOutcome Outcome);
