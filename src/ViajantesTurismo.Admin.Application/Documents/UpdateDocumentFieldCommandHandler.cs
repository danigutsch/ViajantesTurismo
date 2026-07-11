using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Updates an explicitly editable document field.</summary>
public sealed class UpdateDocumentFieldCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Applies and persists the permitted staff override.</summary>
    public async Task<Result> Handle(UpdateDocumentFieldCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await documentStore.GetById(command.DocumentId, ct);
        if (document is null)
        {
            return DocumentErrors.DocumentNotFound(command.DocumentId);
        }

        var result = document.UpdateField(command.FieldId, command.Value, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
