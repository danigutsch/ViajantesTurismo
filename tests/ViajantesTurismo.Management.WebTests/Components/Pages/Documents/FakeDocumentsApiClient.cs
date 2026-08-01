using System.Net;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

internal sealed class FakeDocumentsApiClient : IDocumentsApiClient
{
    private readonly Dictionary<Guid, GetDocumentDto> _documents = [];

    public GetDocumentDto? GeneratedDocument { get; set; }

    public Exception? GetDocumentException { get; set; }

    public GetDocumentDto? RegeneratedDocument { get; set; }

    public GetDocumentDto? UpdatedDocument { get; set; }

    public Guid? LastBeginReviewDocumentId { get; private set; }

    public Guid? LastUpdatedDocumentId { get; private set; }

    public string? LastUpdatedFieldId { get; private set; }

    public string? LastUpdatedFieldValue { get; private set; }

    public DocumentArtifactResponse? Artifact { get; set; }

    public Func<Guid, CancellationToken, Task<DocumentArtifactResponse?>>? DownloadArtifactHandler { get; set; }

    public void AddDocument(GetDocumentDto document) => _documents.Add(document.Id, document);

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> GetDocumentById(Guid id, CancellationToken ct) =>
        GetDocumentException is null
            ? Task.FromResult(DocumentOutcome(_documents.GetValueOrDefault(id)))
            : Task.FromResult(TransportFailure());

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> GenerateContractDraft(Guid bookingId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GeneratedDocument, HttpStatusCode.Created));

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> BeginReview(Guid documentId, CancellationToken ct)
    {
        LastBeginReviewDocumentId = documentId;
        return Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));
    }

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> RequestChanges(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> UpdateField(Guid documentId, string fieldId, UpdateDocumentFieldDto dto, CancellationToken ct)
    {
        LastUpdatedDocumentId = documentId;
        LastUpdatedFieldId = fieldId;
        LastUpdatedFieldValue = dto.Value;
        return Task.FromResult(DocumentOutcome(UpdatedDocument ?? GetRequiredDocument(documentId)));
    }

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> Approve(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> Finalize(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> Regenerate(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(RegeneratedDocument ?? GetRequiredDocument(documentId)));

    public Task<ContractCommandOutcomeDto<GetDocumentDto>> Void(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));

    public Task<DocumentArtifactResponse?> DownloadFinalizedArtifact(Guid documentId, CancellationToken ct) =>
        DownloadArtifactHandler is null
            ? Task.FromResult(Artifact)
            : DownloadArtifactHandler(documentId, ct);

    private GetDocumentDto GetRequiredDocument(Guid id) =>
        _documents.GetValueOrDefault(id) ?? throw new InvalidOperationException("The configured document was not found.");

    private static ContractCommandOutcomeDto<GetDocumentDto> DocumentOutcome(
        GetDocumentDto? document,
        HttpStatusCode successStatusCode = HttpStatusCode.OK) =>
        document is null
            ? ContractCommandOutcome.Status<GetDocumentDto>(ContractCommandOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : ContractCommandOutcome.Succeeded(successStatusCode, document);

    private static ContractCommandOutcomeDto<GetDocumentDto> TransportFailure() => new()
    {
        Kind = ContractCommandOutcomeKind.TransportFailure,
        Message = "HTTP transport failed."
    };
}
