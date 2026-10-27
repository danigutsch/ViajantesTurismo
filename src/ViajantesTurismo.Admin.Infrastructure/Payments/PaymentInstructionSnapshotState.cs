using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Serialized state of a payment instruction snapshot; its string form is redacted.</summary>
/// <param name="SchemaVersion">The serialization schema version.</param>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="Revision">The document revision number.</param>
/// <param name="Terms">The payment terms.</param>
/// <param name="Recipient">The payment recipient.</param>
/// <param name="CreatedBy">The actor who created the snapshot.</param>
/// <param name="CreatedAt">When the snapshot was created.</param>
internal sealed record PaymentInstructionSnapshotState(int SchemaVersion, Guid DocumentId, int Revision,
    PaymentInstructionTermsState Terms, PaymentRecipientState Recipient, string CreatedBy, DateTimeOffset CreatedAt)
{
    public override string ToString() => "PaymentInstructionSnapshotState [redacted]";
}
