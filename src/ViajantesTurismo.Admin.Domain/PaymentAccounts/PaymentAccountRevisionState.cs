namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

internal sealed record PaymentAccountRevisionState(
    Guid Id, int Number, PaymentRecipientState Recipient, string CreatedBy, DateTimeOffset CreatedAt,
    string? VerifiedBy, DateTimeOffset? VerifiedAt, PaymentAccountVerificationMethod Method, DateTimeOffset? CheckedAt)
{
    public override string ToString() => "PaymentAccountRevisionState [redacted]";
}
