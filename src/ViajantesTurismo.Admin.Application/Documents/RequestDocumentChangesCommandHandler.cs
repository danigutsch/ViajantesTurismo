using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests changes to a document under staff review.</summary>
public sealed class RequestDocumentChangesCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RequestDocumentChangesCommand, Result>
{
    /// <summary>Records requested changes and persists the status transition.</summary>
    public async ValueTask<Result> Handle(RequestDocumentChangesCommand command, CancellationToken ct)
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

        var result = lineage.RequestChanges(document.Id, now, command.AuditContext);
        if (result.IsFailure)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.StateConflict, document);
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
