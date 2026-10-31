using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Adds metadata-only audit records to the current document unit of work.</summary>
public sealed class DocumentAuditWriter(
    IDocumentAuditStore auditStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Adds and persists an audit record when trusted request metadata is available.</summary>
    public Task<Result> Add(
        DocumentAuditContext auditContext,
        DocumentAuditOperation operation,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct) => Persist(
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
    public Task<Result> AddHold(
        DocumentAuditContext auditContext,
        DocumentAuditOperation operation,
        Guid documentId,
        Guid? holdId,
        string? holdBasis,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct) => Persist(
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

        var recordResult = DocumentAuditRecord.Create(
            auditContext.ActorId,
            documentId,
            bookingId,
            documentRevision,
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
