namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

internal sealed record PaymentAccountState(
    int SchemaVersion, Guid Id, long Version, string Label, PaymentAccountStatus Status,
    PaymentAccountRevisionState[] Revisions, PaymentAccountReviewState[] Reviews)
{
    public override string ToString() => "PaymentAccountState [redacted]";
}
