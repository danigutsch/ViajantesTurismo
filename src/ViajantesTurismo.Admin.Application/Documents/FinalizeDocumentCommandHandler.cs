using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Renders and seals an approved document artifact.</summary>
public sealed class FinalizeDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IDocumentSourceVersionReader sourceVersionReader)
    : ICommandHandler<FinalizeDocumentCommand, Result>
{
    /// <summary>Finalizes the artifact and supersedes older finalized revisions only after success.</summary>
    public async ValueTask<Result> Handle(FinalizeDocumentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.ClearRejectionAudit();
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
            command.DeferRejectionAudit(DocumentAuditReasonCode.DocumentNotFound);
            return DocumentErrors.DocumentNotFound();
        }

        if (document.RequiresPaymentInstructionWorkflow)
        {
            return Domain.Payments.FinancialErrors.Invalid("paymentInstructionLifecycle");
        }

        var stateResult = lineage.EnsureCanFinalize(document.Id);
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

        var result = lineage.Finalize(
            document.Id, DocumentArtifactRenderer.Render(document), now, auditContext, DocumentArtifactRendererVersion.Current);
        if (result.IsFailure)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.StateConflict, document);
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
