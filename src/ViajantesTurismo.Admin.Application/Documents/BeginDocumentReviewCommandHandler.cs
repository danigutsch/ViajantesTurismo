using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Moves an eligible draft into staff review.</summary>
public sealed class BeginDocumentReviewCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IDocumentAuditStore? auditStore = null)
{
    /// <summary>Starts or resumes review and persists the status transition.</summary>
    public async Task<Result> Handle(BeginDocumentReviewCommand command, CancellationToken ct)
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

        var result = document.BeginReview(now);
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
            DocumentAuditOperation.BeginReview,
            document.Id,
            document.BookingId,
            document.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualOperation,
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
            DocumentAuditOperation.BeginReview,
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
