using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Adds metadata-only audit records to the current document unit of work.</summary>
public sealed class DocumentAuditWriter(
    IDocumentAuditStore auditStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Adds and persists an audit record when trusted request metadata is available. A rejection because the document
    /// was not found records no resource identifiers, so every record that names a document can be tied to its lineage.
    /// </summary>
    public async Task<Result> Add(
        DocumentAuditContext auditContext,
        DocumentAuditOperation operation,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct) => await Persist(
            auditContext,
            operation,
            documentId,
            bookingId,
            documentRevision,
            outcome,
            reasonCode,
            null,
            null,
            ct);

    /// <summary>
    /// Adds and persists a hold audit record naming the hold and its basis, so the record commits with whatever the
    /// current unit of work already staged.
    /// </summary>
    public async Task<Result> AddHold(
        DocumentAuditContext auditContext,
        DocumentAuditOperation operation,
        Guid documentId,
        Guid? holdId,
        string? holdBasis,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct) => await Persist(
            auditContext,
            operation,
            documentId,
            null,
            null,
            outcome,
            reasonCode,
            holdId,
            holdBasis,
            ct);

    private async Task<Result> Persist(
        DocumentAuditContext auditContext,
        DocumentAuditOperation operation,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        Guid? holdId,
        string? holdBasis,
        CancellationToken ct)
    {
        if (auditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired();
        }

        var unresolvedDocument = outcome == DocumentAuditOutcome.Rejected && reasonCode == DocumentAuditReasonCode.DocumentNotFound;
        var recordResult = DocumentAuditRecord.Create(
            auditContext.ActorId,
            unresolvedDocument ? null : documentId,
            unresolvedDocument ? null : bookingId,
            unresolvedDocument ? null : documentRevision,
            operation,
            outcome,
            reasonCode,
            auditContext.CorrelationId,
            timeProvider.GetUtcNow().UtcDateTime,
            holdId,
            holdBasis);
        if (recordResult.IsFailure)
        {
            return recordResult.ConvertError();
        }

        auditStore.Add(recordResult.Value);
        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
