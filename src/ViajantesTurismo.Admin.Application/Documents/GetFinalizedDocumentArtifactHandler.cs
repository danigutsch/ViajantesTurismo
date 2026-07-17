using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Retrieves a sealed document artifact for a mediated delivery boundary.</summary>
public sealed class GetFinalizedDocumentArtifactHandler(IDocumentStore documentStore)
{
    /// <summary>Returns the artifact only when the document revision is finalized and sealed.</summary>
    public async Task<Result<FinalizedDocumentArtifact>> Handle(GetFinalizedDocumentArtifactQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var document = await documentStore.GetById(query.DocumentId, ct);
        if (document is null)
        {
            return DocumentErrors.DocumentNotFound(query.DocumentId).ConvertError<FinalizedDocumentArtifact>();
        }

        var artifactContent = document.GetFinalizedArtifactContent();
        if (document.Status != DocumentStatus.Finalized
            || artifactContent is null
            || string.IsNullOrWhiteSpace(document.FinalizedArtifactName))
        {
            return DocumentErrors.FinalizedArtifactNotAvailable().ConvertError<FinalizedDocumentArtifact>();
        }

        return Result.Ok(new FinalizedDocumentArtifact(
            document.Id,
            document.BookingId,
            document.Revision,
            artifactContent.Value,
            document.FinalizedArtifactName));
    }
}
