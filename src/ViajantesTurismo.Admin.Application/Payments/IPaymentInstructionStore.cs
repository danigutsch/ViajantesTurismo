using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Protects immutable instruction snapshots and finalized artifacts with independent Admin purposes.</summary>
public interface IPaymentInstructionStore
{
    /// <summary>Lists retained immutable document revisions for a booking.</summary>
    Task<IReadOnlyList<Guid>> GetDocumentIds(Guid bookingId, CancellationToken ct);
    /// <summary>Lists the retained document identifiers in one page, newest revision first.</summary>
    Task<(IReadOnlyList<Guid> Items, PageWindow Window)> GetDocumentIdPage(Guid bookingId, int? page, int pageSize, CancellationToken ct);
    /// <summary>Reads one instruction revision's current lineage version and retirement state.</summary>
    Task<(long Version, bool IsRetired)?> GetDocumentState(Guid documentId, CancellationToken ct);
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
    /// <summary>Reads the last audit with a matching operation in persisted sequence order.</summary>
    Task<PaymentInstructionAuditMetadata?> GetLatestAudit(Guid documentId, IReadOnlyList<PaymentInstructionAuditOperation> operations, CancellationToken ct);
    /// <summary>Reads one page of independent reviews in recorded order, or null for a missing instruction.</summary>
    Task<(IReadOnlyList<PaymentInstructionAuditMetadata> Items, PageWindow Window)?> GetReviewPage(Guid documentId, int? page, int pageSize, CancellationToken ct);
    /// <summary>Reads protected HTML only for an authorized, freshly revalidated download operation.</summary>
    Task<byte[]?> GetArtifact(Guid documentId, CancellationToken ct);
}
