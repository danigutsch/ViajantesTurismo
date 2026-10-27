namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Persistence snapshot of a payment account aggregate.</summary>
/// <param name="SchemaVersion">The snapshot schema version.</param>
/// <param name="Id">The account identifier.</param>
/// <param name="Version">The optimistic concurrency version.</param>
/// <param name="Label">The account display label.</param>
/// <param name="Status">The account lifecycle status.</param>
/// <param name="Revisions">The recipient revisions, oldest first.</param>
/// <param name="Reviews">The recorded review decisions.</param>
internal sealed record PaymentAccountState(
    int SchemaVersion, Guid Id, long Version, string Label, PaymentAccountStatus Status,
    PaymentAccountRevisionState[] Revisions, PaymentAccountReviewState[] Reviews)
{
    public override string ToString() => "PaymentAccountState [redacted]";
}
