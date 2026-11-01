using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Approves a document after staff review completes.</summary>
public sealed class ApproveDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    DocumentAuditWriter documentAuditWriter,
    IDocumentSourceVersionReader sourceVersionReader)
{
    /// <summary>Approves a reviewable document draft.</summary>
    public async Task<Result> Handle(ApproveDocumentCommand command, CancellationToken ct)
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
                DocumentErrors.DocumentNotFound(),
                command.AuditContext,
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

        var stateResult = lineage.EnsureCanApprove(document.Id);
        if (stateResult.IsFailure)
        {
            return await RecordAndReturn(
                stateResult,
                command.AuditContext,
                document.Id,
                document.BookingId,
                document.Revision,
                DocumentAuditReasonCode.StateConflict,
                ct);
        }

        var sourceVersionRead = await sourceVersionReader.Read(document.BookingId, ct);
        if (sourceVersionRead.Result.IsFailure)
        {
            return await RecordAndReturn(
                sourceVersionRead.Result.ConvertError(),
                command.AuditContext,
                document.Id,
                document.BookingId,
                document.Revision,
                sourceVersionRead.AuditReasonCode,
                ct);
        }

        var freshnessResult = lineage.EnsureSourceIsCurrent(document.Id, sourceVersionRead.Result.Value);
        if (freshnessResult.IsFailure)
        {
            return await RecordAndReturn(
                freshnessResult,
                command.AuditContext,
                document.Id,
                document.BookingId,
                document.Revision,
                DocumentAuditReasonCode.StateConflict,
                ct);
        }

        var result = lineage.Approve(document.Id, now, command.AuditContext);
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
        DocumentAuditContext auditContext,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct)
    {
        var auditResult = await documentAuditWriter.Add(
            auditContext,
            DocumentAuditOperation.Approve,
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
