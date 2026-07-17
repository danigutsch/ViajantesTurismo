using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Renders and seals an approved document artifact.</summary>
public sealed class FinalizeDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IDocumentAuditStore? auditStore = null)
{
    /// <summary>Finalizes the artifact and supersedes its predecessor only after success.</summary>
    public async Task<Result> Handle(FinalizeDocumentCommand command, CancellationToken ct)
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

        var result = document.Finalize(DocumentArtifactRenderer.Render(document), now);
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

        if (document.ReplacesDocumentId is Guid previousDocumentId)
        {
            var previous = await documentStore.GetById(previousDocumentId, ct);
            if (previous is not null && previous.Status == DocumentStatus.Finalized)
            {
                var supersedeResult = previous.Supersede(now);
                if (supersedeResult.IsFailure)
                {
                    return await RecordAndReturn(
                        supersedeResult,
                        command.AuditContext,
                        document.Id,
                        document.BookingId,
                        document.Revision,
                        DocumentAuditReasonCode.StateConflict,
                        now,
                        ct);
                }
            }
        }

        var auditResult = DocumentAuditWriter.Add(
            auditStore,
            command.AuditContext,
            DocumentAuditOperation.Finalize,
            document.Id,
            document.BookingId,
            document.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualFinalize,
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
            DocumentAuditOperation.Finalize,
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
