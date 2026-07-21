using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests changes to a document under staff review.</summary>
public sealed class RequestDocumentChangesCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    DocumentAuditWriter documentAuditWriter)
{
    /// <summary>Records requested changes and persists the status transition.</summary>
    public async Task<Result> Handle(RequestDocumentChangesCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.AuditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lineage = await documentStore.GetByDocumentId(command.DocumentId, ct);
        var document = lineage?.GetRevision(command.DocumentId);
        if (lineage is null || document is null)
        {
            return await RecordAndReturn(
                DocumentErrors.DocumentNotFound(command.DocumentId),
                command.AuditContext,
                command.DocumentId,
                null,
                null,
                DocumentAuditReasonCode.DocumentNotFound,
                ct);
        }

        var result = lineage.RequestChanges(document.Id, now, command.AuditContext);
        if (result.IsFailure)
        {
            return await RecordAndReturn(
                result,
                command.AuditContext,
                document.Id,
                document.BookingId,
                document.Revision,
                DocumentAuditReasonCode.StateConflict,
                ct);
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }

    private async Task<Result> RecordAndReturn(
        Result operationResult,
        DocumentAuditContext? auditContext,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct)
    {
        var auditResult = await documentAuditWriter.Add(
            auditContext,
            DocumentAuditOperation.RequestChanges,
            documentId,
            bookingId,
            documentRevision,
            DocumentAuditOutcome.Rejected,
            reasonCode,
            ct);
        if (auditResult.IsFailure)
        {
            return auditResult;
        }

        return operationResult;
    }
}
