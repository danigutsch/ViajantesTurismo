using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Voids a document revision without changing its finalized artifact.</summary>
public sealed class VoidDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IDocumentAuditStore? auditStore = null)
{
    /// <summary>Records a bounded staff reason and persists the voided status.</summary>
    public async Task<Result> Handle(VoidDocumentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var document = await documentStore.GetById(command.DocumentId, ct);
        if (document is null)
        {
            return await RecordAndReturn(
                DocumentErrors.DocumentNotFound(command.DocumentId),
                command.AuditContext,
                command.DocumentId,
                null,
                null,
                DocumentAuditReasonCode.DocumentNotFound,
                now,
                ct);
        }

        var result = document.Void(command.Reason, now);
        if (result.IsFailure)
        {
            return await RecordAndReturn(
                result,
                command.AuditContext,
                document.Id,
                document.BookingId,
                document.Revision,
                DocumentAuditReasonCode.StateConflict,
                now,
                ct);
        }

        var auditResult = DocumentAuditWriter.Add(
            auditStore,
            command.AuditContext,
            DocumentAuditOperation.Void,
            document.Id,
            document.BookingId,
            document.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualVoid,
            now);
        if (auditResult.IsFailure)
        {
            return auditResult.ConvertError();
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
        DateTime occurredAtUtc,
        CancellationToken ct)
    {
        var auditResult = DocumentAuditWriter.Add(
            auditStore,
            auditContext,
            DocumentAuditOperation.Void,
            documentId,
            bookingId,
            documentRevision,
            DocumentAuditOutcome.Rejected,
            reasonCode,
            occurredAtUtc);
        if (auditResult.IsFailure)
        {
            return auditResult.ConvertError();
        }

        if (auditResult.Value)
        {
            await unitOfWork.SaveEntities(ct);
        }

        return operationResult;
    }
}
