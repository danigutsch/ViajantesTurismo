using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed record PaymentInstructionSnapshotState(int SchemaVersion, Guid DocumentId, int Revision,
    PaymentInstructionTermsState Terms, PaymentRecipientState Recipient, string CreatedBy, DateTimeOffset CreatedAt)
{
    public override string ToString() => "PaymentInstructionSnapshotState [redacted]";
}
