using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Voids a document revision without changing its finalized artifact.</summary>
public sealed class VoidDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Records a bounded staff reason and persists the voided status.</summary>
    public async Task<Result> Handle(VoidDocumentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await documentStore.GetById(command.DocumentId, ct);
        if (document is null)
        {
            return DocumentErrors.DocumentNotFound(command.DocumentId);
        }

        var result = document.Void(command.Reason, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
