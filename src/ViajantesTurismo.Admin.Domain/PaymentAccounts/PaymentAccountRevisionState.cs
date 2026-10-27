namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Persistence snapshot of a payment account recipient revision and its verification evidence.</summary>
/// <param name="Id">The immutable revision identifier.</param>
/// <param name="Number">The monotonically increasing revision number.</param>
/// <param name="Recipient">The persisted recipient details.</param>
/// <param name="CreatedBy">The opaque identifier of the Admin who created the revision.</param>
/// <param name="CreatedAt">The UTC creation instant.</param>
/// <param name="VerifiedBy">The opaque identifier of the verifying Admin, or null when not verified.</param>
/// <param name="VerifiedAt">The UTC verification instant, or null when not verified.</param>
/// <param name="Method">The method used to independently check the recipient.</param>
/// <param name="CheckedAt">The attested check instant, or null when no check was recorded.</param>
internal sealed record PaymentAccountRevisionState(
    Guid Id, int Number, PaymentRecipientState Recipient, string CreatedBy, DateTimeOffset CreatedAt,
    string? VerifiedBy, DateTimeOffset? VerifiedAt, PaymentAccountVerificationMethod Method, DateTimeOffset? CheckedAt)
{
    public override string ToString() => "PaymentAccountRevisionState [redacted]";
}
