using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Approves a document after staff review completes.</summary>
public sealed class ApproveDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Approves a reviewable document draft.</summary>
    public async Task<Result> Handle(ApproveDocumentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await documentStore.GetById(command.DocumentId, ct);
        if (document is null)
        {
            return DocumentErrors.DocumentNotFound(command.DocumentId);
        }

        var result = document.Approve(timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
