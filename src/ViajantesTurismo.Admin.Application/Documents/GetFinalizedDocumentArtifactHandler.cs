using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Retrieves a sealed document artifact for a mediated delivery boundary.</summary>
public sealed class GetFinalizedDocumentArtifactHandler(IDocumentStore documentStore, PaymentInstructionWorkflow? paymentInstructions = null)
{
    /// <summary>Returns the artifact only when the document revision is finalized and sealed.</summary>
    public async Task<Result<FinalizedDocumentArtifact>> Handle(GetFinalizedDocumentArtifactQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var lineage = await documentStore.GetByDocumentId(query.DocumentId, ct);
        var document = lineage?.GetRevision(query.DocumentId);
        if (document is null)
        {
            return DocumentErrors.DocumentNotFound().ConvertError<FinalizedDocumentArtifact>();
        }

        if (document.RequiresPaymentInstructionWorkflow)
        {
            return paymentInstructions is not null ? await paymentInstructions.Download(query.DocumentId, query.ActorId, ct)
                : DocumentErrors.FinalizedArtifactNotAvailable().ConvertError<FinalizedDocumentArtifact>();
        }
        var artifactContent = document.CopyFinalizedArtifactContent();
        if (!document.HasAvailableFinalizedArtifact || artifactContent is null || document.FinalizedArtifactName is null)
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
