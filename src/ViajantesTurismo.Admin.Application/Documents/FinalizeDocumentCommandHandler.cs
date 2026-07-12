using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Renders and seals an approved document artifact.</summary>
public sealed class FinalizeDocumentCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Finalizes the artifact and supersedes its predecessor only after success.</summary>
    public async Task<Result> Handle(FinalizeDocumentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await documentStore.GetById(command.DocumentId, ct);
        if (document is null)
        {
            return DocumentErrors.DocumentNotFound(command.DocumentId);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var result = document.Finalize(DocumentArtifactRenderer.Render(document), now);
        if (result.IsFailure)
        {
            return result;
        }

        if (document.ReplacesDocumentId is Guid previousDocumentId)
        {
            var previous = await documentStore.GetById(previousDocumentId, ct);
            if (previous is not null && previous.Status == DocumentStatus.Finalized)
            {
                var supersedeResult = previous.Supersede(now);
                if (supersedeResult.IsFailure)
                {
                    return supersedeResult;
                }
            }
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
