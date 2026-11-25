using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Voids a document revision without changing its finalized artifact.</summary>
public sealed class VoidDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<VoidDocumentCommand, Result>
{
    /// <summary>Records a bounded staff reason and persists the voided status.</summary>
    public async ValueTask<Result> Handle(VoidDocumentCommand command, CancellationToken ct)
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

        var result = lineage.Void(document.Id, command.Reason, now, command.AuditContext);
        if (result.IsFailure)
        {
            command.DeferRejectionAudit(
                result.Status == ResultStatus.Invalid
                    ? DocumentAuditReasonCode.ValidationRejected
                    : DocumentAuditReasonCode.StateConflict,
                document);
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
