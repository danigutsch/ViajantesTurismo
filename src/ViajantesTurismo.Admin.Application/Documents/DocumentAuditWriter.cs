using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Adds metadata-only audit records to the current document unit of work.</summary>
public sealed class DocumentAuditWriter(
    IDocumentAuditStore auditStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Adds and persists an audit record when trusted request metadata is available.</summary>
    public async Task<Result> Add(
        DocumentAuditContext? auditContext,
        DocumentAuditOperation operation,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
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
            timeProvider.GetUtcNow().UtcDateTime);
        if (recordResult.IsFailure)
        {
            return recordResult.ConvertError();
        }

        auditStore.Add(recordResult.Value);
        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
