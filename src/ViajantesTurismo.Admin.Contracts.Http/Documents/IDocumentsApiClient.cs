using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Provides typed access to Admin generated-document operations.</summary>
public interface IDocumentsApiClient
{
    /// <summary>Gets a generated document revision by identifier.</summary>
    Task<HttpOutcome<GetDocumentDto>> GetDocumentById(Guid id, CancellationToken ct);

    /// <summary>Generates the server-selected contract draft for an eligible booking.</summary>
    Task<HttpOutcome<GetDocumentDto>> GenerateContractDraft(Guid bookingId, CancellationToken ct);

    /// <summary>Generates a contract draft using a stable key retained by the caller across retries.</summary>
    /// <param name="bookingId">The booking whose contract draft is generated.</param>
    /// <param name="idempotencyKey">The same opaque key for every retry of this logical command.</param>
    /// <param name="ct">The caller cancellation token.</param>
    /// <returns>The document command outcome.</returns>
    /// <remarks>Implementations that do not support caller-provided keys fail explicitly instead of discarding the key.</remarks>
    Task<HttpOutcome<GetDocumentDto>> GenerateContractDraft(Guid bookingId, string idempotencyKey, CancellationToken ct) =>
        throw new NotSupportedException("This client does not support caller-provided document idempotency keys.");

    /// <summary>Starts or resumes staff review of a document draft.</summary>
    Task<HttpOutcome<GetDocumentDto>> BeginReview(Guid documentId, CancellationToken ct);

    /// <summary>Requests changes to a document draft.</summary>
    Task<HttpOutcome<GetDocumentDto>> RequestChanges(Guid documentId, CancellationToken ct);

    /// <summary>Updates one staff-editable document field.</summary>
    Task<HttpOutcome<GetDocumentDto>> UpdateField(Guid documentId, string fieldId, UpdateDocumentFieldDto dto, CancellationToken ct);

    /// <summary>Approves a document draft for finalization.</summary>
    Task<HttpOutcome<GetDocumentDto>> Approve(Guid documentId, CancellationToken ct);

    /// <summary>Finalizes a document artifact.</summary>
    Task<HttpOutcome<GetDocumentDto>> Finalize(Guid documentId, CancellationToken ct);

    /// <summary>Generates a replacement document revision from current source data.</summary>
    Task<HttpOutcome<GetDocumentDto>> Regenerate(Guid documentId, CancellationToken ct);

    /// <summary>Generates a replacement revision using a stable key retained by the caller across retries.</summary>
    /// <param name="documentId">The document whose replacement revision is generated.</param>
    /// <param name="idempotencyKey">The same opaque key for every retry of this logical command.</param>
    /// <param name="ct">The caller cancellation token.</param>
    /// <returns>The document command outcome.</returns>
    /// <remarks>Implementations that do not support caller-provided keys fail explicitly instead of discarding the key.</remarks>
    Task<HttpOutcome<GetDocumentDto>> Regenerate(Guid documentId, string idempotencyKey, CancellationToken ct) =>
        throw new NotSupportedException("This client does not support caller-provided document idempotency keys.");

    /// <summary>Voids a document using the server-defined bounded reason code.</summary>
    Task<HttpOutcome<GetDocumentDto>> Void(Guid documentId, CancellationToken ct);

    /// <summary>Downloads a finalized HTML artifact through the Admin API.</summary>
    Task<DocumentArtifactResponse?> DownloadFinalizedArtifact(Guid documentId, CancellationToken ct);
}
