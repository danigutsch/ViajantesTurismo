using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Contains a protected instruction's immutable in-memory financial and recipient snapshots.</summary>
public sealed record PaymentInstructionSnapshot(Guid DocumentId, int Revision, FrozenPaymentInstruction Terms, PaymentRecipientDetails Recipient, string CreatedBy, DateTimeOffset CreatedAt, DateTimeOffset? FinalizedAt = null)
{
    /// <inheritdoc />
    public override string ToString() => "PaymentInstructionSnapshot [redacted]";
}
