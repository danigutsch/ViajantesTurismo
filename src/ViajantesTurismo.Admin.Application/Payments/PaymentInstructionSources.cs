using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Payments;

internal sealed record PaymentInstructionSources(DocumentLineage Lineage, DocumentDraft Document, PaymentInstructionSnapshot Snapshot)
{
    public override string ToString() => "PaymentInstructionSources [redacted]";
}
