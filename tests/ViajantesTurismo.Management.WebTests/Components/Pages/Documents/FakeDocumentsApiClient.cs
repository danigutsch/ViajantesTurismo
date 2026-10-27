using System.Net;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

/// <summary>In-memory documents API client that records calls and returns configured documents.</summary>
internal sealed class FakeDocumentsApiClient : IDocumentsApiClient
{
    private readonly Dictionary<Guid, GetDocumentDto> _documents = [];

    /// <summary>Document returned by a generate call.</summary>
    public GetDocumentDto? GeneratedDocument
    {
        get; set;
    }

    /// <summary>When set, a get call returns a transport failure.</summary>
    public Exception? GetDocumentException
    {
        get; set;
    }

    /// <summary>Document returned by a regenerate call when no outcome is configured.</summary>
    public GetDocumentDto? RegeneratedDocument
    {
        get; set;
    }

    /// <summary>Outcome returned by a regenerate call, overriding the document.</summary>
    public HttpOutcome<GetDocumentDto>? RegenerationOutcome
    {
        get; set;
    }

    /// <summary>Idempotency keys received by generate calls.</summary>
    public List<string> GenerationKeys { get; } = [];

    /// <summary>Idempotency keys received by regenerate calls.</summary>
    public List<string> RegenerationKeys { get; } = [];

    /// <summary>Document returned by a field update instead of the stored one.</summary>
    public GetDocumentDto? UpdatedDocument
    {
        get; set;
    }

    /// <summary>Id passed to the last begin-review call.</summary>
    public Guid? LastBeginReviewDocumentId
    {
        get; private set;
    }

    /// <summary>Id passed to the last field update.</summary>
    public Guid? LastUpdatedDocumentId
    {
        get; private set;
    }

    /// <summary>Field id passed to the last field update.</summary>
    public string? LastUpdatedFieldId
    {
        get; private set;
    }

    /// <summary>Value passed to the last field update.</summary>
    public string? LastUpdatedFieldValue
    {
        get; private set;
    }

    /// <summary>Artifact returned by download when no handler is set.</summary>
    public DocumentArtifactResponse? Artifact
    {
        get; set;
    }

    /// <summary>Optional handler that replaces the default artifact download.</summary>
    public Func<Guid, CancellationToken, Task<DocumentArtifactResponse?>>? DownloadArtifactHandler
    {
        get; set;
    }

    /// <summary>Stores a document, keyed by its id, for later lookups.</summary>
    public void AddDocument(GetDocumentDto document) => _documents.Add(document.Id, document);

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> GetDocumentById(Guid id, CancellationToken ct) =>
        GetDocumentException is null
            ? Task.FromResult(DocumentOutcome(_documents.GetValueOrDefault(id)))
            : Task.FromResult(TransportFailure());

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> GenerateContractDraft(Guid bookingId, string idempotencyKey, CancellationToken ct)
    {
        GenerationKeys.Add(idempotencyKey);
        return Task.FromResult(DocumentOutcome(GeneratedDocument, HttpStatusCode.Created));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> BeginReview(Guid documentId, CancellationToken ct)
    {
        LastBeginReviewDocumentId = documentId;
        return Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> RequestChanges(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> UpdateField(Guid documentId, string fieldId, UpdateDocumentFieldDto dto, CancellationToken ct)
    {
        LastUpdatedDocumentId = documentId;
        LastUpdatedFieldId = fieldId;
        LastUpdatedFieldValue = dto.Value;
        return Task.FromResult(DocumentOutcome(UpdatedDocument ?? GetRequiredDocument(documentId)));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> Approve(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> Finalize(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> Regenerate(Guid documentId, string idempotencyKey, CancellationToken ct)
    {
        RegenerationKeys.Add(idempotencyKey);
        return Task.FromResult(RegenerationOutcome ?? DocumentOutcome(RegeneratedDocument ?? GetRequiredDocument(documentId)));
    }

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentDto>> Void(Guid documentId, CancellationToken ct) =>
        Task.FromResult(DocumentOutcome(GetRequiredDocument(documentId)));

    /// <inheritdoc />
    public Task<DocumentArtifactResponse?> DownloadFinalizedArtifact(Guid documentId, CancellationToken ct) =>
        DownloadArtifactHandler is null
            ? Task.FromResult(Artifact)
            : DownloadArtifactHandler(documentId, ct);

    private GetDocumentDto GetRequiredDocument(Guid id) =>
        _documents.GetValueOrDefault(id) ?? throw new InvalidOperationException("The configured document was not found.");

    private static HttpOutcome<GetDocumentDto> DocumentOutcome(
        GetDocumentDto? document,
        HttpStatusCode successStatusCode = HttpStatusCode.OK) =>
        document is null
            ? HttpOutcomeMapper.Status<GetDocumentDto>(HttpOutcomeKind.NotFound, HttpStatusCode.NotFound)
            : HttpOutcomeMapper.Succeeded(successStatusCode, document);

    private static HttpOutcome<GetDocumentDto> TransportFailure() => new()
    {
        Kind = HttpOutcomeKind.TransportFailure,
        Message = "HTTP transport failed."
    };
}
