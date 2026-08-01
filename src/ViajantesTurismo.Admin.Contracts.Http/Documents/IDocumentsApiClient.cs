using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Provides typed access to Admin generated-document operations.</summary>
public interface IDocumentsApiClient
{
    /// <summary>Gets a generated document revision by identifier.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> GetDocumentById(Guid id, CancellationToken ct);

    /// <summary>Generates the server-selected contract draft for an eligible booking.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> GenerateContractDraft(Guid bookingId, CancellationToken ct);

    /// <summary>Starts or resumes staff review of a document draft.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> BeginReview(Guid documentId, CancellationToken ct);

    /// <summary>Requests changes to a document draft.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> RequestChanges(Guid documentId, CancellationToken ct);

    /// <summary>Updates one staff-editable document field.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> UpdateField(Guid documentId, string fieldId, UpdateDocumentFieldDto dto, CancellationToken ct);

    /// <summary>Approves a document draft for finalization.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> Approve(Guid documentId, CancellationToken ct);

    /// <summary>Finalizes a document artifact.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> Finalize(Guid documentId, CancellationToken ct);

    /// <summary>Generates a replacement document revision from current source data.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> Regenerate(Guid documentId, CancellationToken ct);

    /// <summary>Voids a document using the server-defined bounded reason code.</summary>
    Task<ContractCommandOutcomeDto<GetDocumentDto>> Void(Guid documentId, CancellationToken ct);

    /// <summary>Downloads a finalized HTML artifact through the Admin API.</summary>
    Task<DocumentArtifactResponse?> DownloadFinalizedArtifact(Guid documentId, CancellationToken ct);
}
