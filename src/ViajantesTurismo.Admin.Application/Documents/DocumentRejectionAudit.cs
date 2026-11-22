using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Captures the metadata needed to audit a document rejection after its transaction rolls back.</summary>
/// <param name="ReasonCode">The bounded reason for the rejection.</param>
/// <param name="DocumentId">The identifier of the document found by the command.</param>
/// <param name="BookingId">The identifier of the document's booking.</param>
/// <param name="DocumentRevision">The revision the command inspected.</param>
/// <param name="DocumentLineageId">The lineage identified by the command before rollback.</param>
/// <param name="HoldBasis">The bounded basis name when a hold placement was rejected.</param>
/// <param name="HoldId">The hold identifier when known.</param>
public sealed record DocumentRejectionAudit(
    DocumentAuditReasonCode ReasonCode,
    Guid? DocumentId = null,
    Guid? BookingId = null,
    int? DocumentRevision = null,
    Guid? DocumentLineageId = null,
    string? HoldBasis = null,
    Guid? HoldId = null);
