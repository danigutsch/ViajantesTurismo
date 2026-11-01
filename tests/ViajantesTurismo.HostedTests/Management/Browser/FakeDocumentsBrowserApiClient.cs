using System.Net;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Documents API client for browser journeys: serves one document with one restraining hold and releases it on request.</summary>
/// <param name="document">The document every read returns.</param>
/// <param name="hold">The restraining hold of the document's lineage.</param>
internal sealed class FakeDocumentsBrowserApiClient(GetDocumentDto document, GetDocumentHoldDto hold) : IDocumentsApiClient
{
    /// <summary>Gets the hold as the fake currently knows it.</summary>
    public GetDocumentHoldDto Hold { get; private set; } = hold;

    /// <summary>Gets the reason codes received by release calls.</summary>
    public List<string> ReleaseReasonCodes { get; } = [];

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> GetDocumentById(Guid id, CancellationToken ct) =>
        Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, document));

    /// <inheritdoc />
    public Task<HttpOutcome<IReadOnlyList<GetDocumentHoldDto>>> ListHolds(Guid documentId, CancellationToken ct) =>
        Task.FromResult(HttpOutcomeMapper.Succeeded<IReadOnlyList<GetDocumentHoldDto>>(HttpStatusCode.OK, [Hold]));

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentHoldDto>> ReleaseHold(Guid documentId, Guid holdId, ReleaseDocumentHoldDto dto, CancellationToken ct)
    {
        ReleaseReasonCodes.Add(dto.ReasonCode);
        Hold = Hold with
        {
            IsRestraining = false,
            ReleasedBy = "actor-release",
            ReleasedAt = TimeProvider.System.GetUtcNow(),
            ReleaseReasonCode = dto.ReasonCode
        };
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Hold));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> GenerateContractDraft(Guid bookingId, string idempotencyKey, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> BeginReview(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> RequestChanges(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> UpdateField(Guid documentId, string fieldId, UpdateDocumentFieldDto dto, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> Approve(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> Finalize(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> Regenerate(Guid documentId, string idempotencyKey, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> Void(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<DocumentArtifactResponse?> DownloadFinalizedArtifact(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentHoldDto>> PlaceHold(Guid documentId, CreateDocumentHoldDto dto, CancellationToken ct) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentHoldOverviewPageDto>> ListHoldOverview(DocumentHoldOverviewQuery query, CancellationToken ct) => throw new NotSupportedException();
}
