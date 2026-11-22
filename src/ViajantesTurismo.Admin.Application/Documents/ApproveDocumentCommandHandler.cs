using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Approves a document after staff review completes.</summary>
public sealed class ApproveDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IDocumentSourceVersionReader sourceVersionReader)
    : ICommandHandler<ApproveDocumentCommand, Result>
{
    /// <summary>Approves a reviewable document draft.</summary>
    public async ValueTask<Result> Handle(ApproveDocumentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.ClearRejectionAudit();
        if (command.AuditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lineage = await documentStore.GetByDocumentId(command.DocumentId, ct);
        var document = lineage?.GetRevision(command.DocumentId);
        if (lineage is null || document is null)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.DocumentNotFound);
            return DocumentErrors.DocumentNotFound();
        }

        if (document.RequiresPaymentInstructionWorkflow)
        {
            return Domain.Payments.FinancialErrors.Invalid("paymentInstructionLifecycle");
        }

        var stateResult = lineage.EnsureCanApprove(document.Id);
        if (stateResult.IsFailure)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.StateConflict, document);
            return stateResult;
        }

        var sourceVersionRead = await sourceVersionReader.Read(document.BookingId, ct);
        if (sourceVersionRead.Result.IsFailure)
        {
            command.DeferRejectionAudit(sourceVersionRead.AuditReasonCode, document);
            return sourceVersionRead.Result.ConvertError();
        }

        var freshnessResult = lineage.EnsureSourceIsCurrent(document.Id, sourceVersionRead.Result.Value);
        if (freshnessResult.IsFailure)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.StateConflict, document);
            return freshnessResult;
        }

        var templateResult = lineage.EnsureTemplateIsCurrent(document.Id);
        if (templateResult.IsFailure)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.StateConflict, document);
            return templateResult;
        }

        var result = lineage.Approve(document.Id, now, command.AuditContext);
        if (result.IsFailure)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.StateConflict, document);
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
