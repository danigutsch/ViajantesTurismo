using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Renders and seals an approved document artifact.</summary>
public sealed class FinalizeDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    DocumentAuditWriter documentAuditWriter,
    IDocumentSourceVersionReader sourceVersionReader)
{
    /// <summary>Finalizes the artifact and supersedes older finalized revisions only after success.</summary>
    public async Task<Result> Handle(FinalizeDocumentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var auditContext = command.AuditContext;
        if (auditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lineage = await documentStore.GetByDocumentId(command.DocumentId, ct);
        var document = lineage?.GetRevision(command.DocumentId);
        if (lineage is null || document is null)
        {
            return await RecordAndReturn(
                DocumentErrors.DocumentNotFound(),
                auditContext,
                command.DocumentId,
                null,
                null,
                DocumentAuditReasonCode.DocumentNotFound,
                ct);
        }

        if (document.RequiresPaymentInstructionWorkflow)
        {
            return Domain.Payments.FinancialErrors.Invalid("paymentInstructionLifecycle");
        }

        var sourceVersionResult = await sourceVersionReader.Read(document.BookingId, ct);
        if (sourceVersionResult.IsFailure)
        {
            return await RecordAndReturn(
                sourceVersionResult.ConvertError(),
                auditContext,
                document.Id,
                document.BookingId,
                document.Revision,
                DocumentAuditReasonCode.BookingNotFound,
                ct);
        }

        var freshnessResult = lineage.EnsureSourceIsCurrent(document.Id, sourceVersionResult.Value);
        if (freshnessResult.IsFailure)
        {
            return await RecordAndReturn(
                freshnessResult,
                auditContext,
                document.Id,
                document.BookingId,
                document.Revision,
                DocumentAuditReasonCode.StateConflict,
                ct);
        }

        var result = lineage.Finalize(document.Id, DocumentArtifactRenderer.Render(document), now, auditContext);
        if (result.IsFailure)
        {
            return await RecordAndReturn(
                result,
                auditContext,
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
        DocumentAuditContext auditContext,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct)
    {
        var auditResult = await documentAuditWriter.Add(
            auditContext,
            DocumentAuditOperation.Finalize,
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
