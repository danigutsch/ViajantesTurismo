namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

internal sealed class FakeDocumentsApiClient : IDocumentsApiClient
{
    private readonly Dictionary<Guid, GetDocumentDto> _documents = [];

    public GetDocumentDto? GeneratedDocument { get; set; }

    public Exception? GetDocumentException { get; set; }

    public GetDocumentDto? RegeneratedDocument { get; set; }

    public Guid? LastBeginReviewDocumentId { get; private set; }

    public DocumentArtifactResponse? Artifact { get; set; }

    public void AddDocument(GetDocumentDto document) => _documents.Add(document.Id, document);

    public Task<GetDocumentDto?> GetDocumentById(Guid id, CancellationToken ct) =>
        GetDocumentException is null
            ? Task.FromResult(_documents.GetValueOrDefault(id))
            : Task.FromException<GetDocumentDto?>(GetDocumentException);

    public Task<GetDocumentDto> GenerateContractDraft(Guid bookingId, CancellationToken ct) =>
        Task.FromResult(GeneratedDocument ?? throw new InvalidOperationException("No generated document was configured."));

    public Task<GetDocumentDto> BeginReview(Guid documentId, CancellationToken ct)
    {
        LastBeginReviewDocumentId = documentId;
        return Task.FromResult(GetRequiredDocument(documentId));
    }

    public Task<GetDocumentDto> RequestChanges(Guid documentId, CancellationToken ct) =>
        Task.FromResult(GetRequiredDocument(documentId));

    public Task<GetDocumentDto> UpdateField(Guid documentId, string fieldId, UpdateDocumentFieldDto dto, CancellationToken ct) =>
        Task.FromResult(GetRequiredDocument(documentId));

    public Task<GetDocumentDto> Approve(Guid documentId, CancellationToken ct) =>
        Task.FromResult(GetRequiredDocument(documentId));

    public Task<GetDocumentDto> Finalize(Guid documentId, CancellationToken ct) =>
        Task.FromResult(GetRequiredDocument(documentId));

    public Task<GetDocumentDto> Regenerate(Guid documentId, CancellationToken ct) =>
        Task.FromResult(RegeneratedDocument ?? GetRequiredDocument(documentId));

    public Task<GetDocumentDto> Void(Guid documentId, CancellationToken ct) =>
        Task.FromResult(GetRequiredDocument(documentId));

    public Task<DocumentArtifactResponse?> DownloadFinalizedArtifact(Guid documentId, CancellationToken ct) =>
        Task.FromResult(Artifact);

    private GetDocumentDto GetRequiredDocument(Guid id) =>
        _documents.GetValueOrDefault(id) ?? throw new InvalidOperationException("The configured document was not found.");
}
