using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Adds metadata-only audit records to the current document unit of work.</summary>
public static class DocumentAuditWriter
{
    /// <summary>Adds an audit record when trusted request metadata is available.</summary>
    public static Result<bool> Add(
        IDocumentAuditStore? auditStore,
        DocumentAuditContext? auditContext,
        DocumentAuditOperation operation,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        DateTime occurredAtUtc)
    {
        if (auditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<bool>();
        }

        if (auditStore is null)
        {
            return DocumentAuditErrors.AuditStoreUnavailable().ConvertError<bool>();
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
            occurredAtUtc);
        if (recordResult.IsFailure)
        {
            return recordResult.ConvertError<DocumentAuditRecord, bool>();
        }

        auditStore.Add(recordResult.Value);
        return Result.Ok(true);
    }
}
