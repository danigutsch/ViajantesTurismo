using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Groups the document, lineage and protected snapshot used to render payment instructions.</summary>
/// <param name="Lineage">Lineage the document belongs to.</param>
/// <param name="Document">Document draft being rendered.</param>
/// <param name="Snapshot">Protected payment instruction snapshot.</param>
internal sealed record PaymentInstructionSources(DocumentLineage Lineage, DocumentDraft Document, PaymentInstructionSnapshot Snapshot)
{
    public override string ToString() => "PaymentInstructionSources [redacted]";
}
