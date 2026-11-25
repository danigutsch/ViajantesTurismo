using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Updates an explicitly editable document field.</summary>
public sealed class UpdateDocumentFieldCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateDocumentFieldCommand, Result>
{
    /// <summary>Applies and persists the permitted staff override.</summary>
    public async ValueTask<Result> Handle(UpdateDocumentFieldCommand command, CancellationToken ct)
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

        if (command.Value is null)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.ValidationRejected, document);
            return DocumentErrors.ValueRequired("value");
        }

        var result = lineage.UpdateField(document.Id, command.FieldId, command.Value, now, command.AuditContext);
        if (result.IsFailure)
        {
            command.DeferRejectionAudit(
                result.Status == ResultStatus.Conflict
                    ? DocumentAuditReasonCode.StateConflict
                    : DocumentAuditReasonCode.ValidationRejected,
                document);
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
