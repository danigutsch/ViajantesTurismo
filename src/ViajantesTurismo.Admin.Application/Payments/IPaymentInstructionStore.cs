using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Protects immutable instruction snapshots and finalized artifacts with independent Admin purposes.</summary>
public interface IPaymentInstructionStore
{
    /// <summary>Lists retained immutable document revisions for a booking.</summary>
    Task<IReadOnlyList<Guid>> GetDocumentIds(Guid bookingId, CancellationToken ct);
    /// <summary>Reads a verified protected snapshot; never falls back to unprotected data.</summary>
    Task<PaymentInstructionSnapshot?> GetSnapshot(Guid documentId, CancellationToken ct);
    /// <summary>Stages a new immutable snapshot linked to existing document lineage.</summary>
    Task SaveSnapshot(DocumentDraft document, FrozenPaymentInstruction terms, PaymentAccount account, string actorId, DateTimeOffset now, CancellationToken ct);
    /// <summary>Stages a protected immutable HTML artifact after fresh eligibility checks.</summary>
    byte[] ProtectArtifact(Guid documentId, byte[] artifact);
    /// <summary>Stages metadata-only instruction audit evidence in the command transaction.</summary>
    void AddAudit(Guid bookingId, PaymentInstructionAuditMetadata metadata);
    /// <summary>Reads immutable instruction audit metadata for approval and independent-review projection.</summary>
    Task<IReadOnlyList<PaymentInstructionAuditMetadata>> GetAudits(Guid documentId, CancellationToken ct);
    /// <summary>Reads protected HTML only for an authorized, freshly revalidated download operation.</summary>
    Task<byte[]?> GetArtifact(Guid documentId, CancellationToken ct);
}
