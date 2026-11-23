using System.Collections.Concurrent;
using System.Net;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Supplies a large synthetic Catalog album using the Management client's wire format.</summary>
internal sealed class GalleryEditorCatalogStub(bool longLabels = false, bool requiresInitialization = false) : HttpMessageHandler
{
    private readonly Guid tourId = Guid.CreateVersion7();
    private readonly Guid intentId = Guid.CreateVersion7();
    private readonly CatalogGalleryPhotoOptionDto[] images = [.. Enumerable.Range(1, 803).Select(number =>
        new CatalogGalleryPhotoOptionDto(Guid.CreateVersion7(), $"Photo {number:D3}" + (longLabels ? new string('界', 72) : ""), 320, "image/jpeg"))];
    private readonly ConcurrentQueue<CatalogGalleryPhotoOptionsRequest> choiceRequests = new();
    private readonly ConcurrentQueue<string> editorRequests = new();
    private readonly ConcurrentQueue<QueueCatalogGalleryPlacementRequest> writeRequests = new();
    private int initializationCount;

    /// <summary>Gets the actual Management editor route.</summary>
    public string EditorPage => $"/catalog/tours/{tourId}/gallery-editor";

    /// <summary>Gets the Catalog search and hydration requests made by the editor.</summary>
    public IReadOnlyList<CatalogGalleryPhotoOptionsRequest> ChoiceRequests => [.. choiceRequests];

    /// <summary>Gets the queries used to retrieve an editor snapshot or queued outcome.</summary>
    public IReadOnlyList<string> EditorRequests => [.. editorRequests];

    /// <summary>Gets the submitted authoring snapshots.</summary>
    public IReadOnlyList<QueueCatalogGalleryPlacementRequest> WriteRequests => [.. writeRequests];

    /// <summary>Gets the number of explicit initialization requests.</summary>
    public int InitializationCount => Volatile.Read(ref initializationCount);

    /// <summary>Looks up a seeded photo ID by its display number.</summary>
    public Guid PhotoKey(int number) => images[number - 1].Id;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Get && request.RequestUri is { } uri && uri.AbsolutePath == $"/api/v1{EditorPage}")
        {
            editorRequests.Enqueue(uri.Query);
            var pending = uri.Query == "?placementSequence=7";
            var waitingForBaseline = requiresInitialization && InitializationCount == 0;
            CatalogGalleryPlacementOutcomeDto? outcome = null;
            if (pending || requiresInitialization)
            {
                var status = pending || waitingForBaseline ? CatalogGalleryPlacementStatusDto.Pending : CatalogGalleryPlacementStatusDto.Applied;
                outcome = new CatalogGalleryPlacementOutcomeDto(intentId, 7, status);
            }

            var state = new CatalogGalleryEditorStateDto(
                3, 3, outcome is null ? 0 : 7, outcome?.Status == CatalogGalleryPlacementStatusDto.Applied ? 7 : 0,
                waitingForBaseline ? null : [.. images.Take(500).Select(image => image.Id)],
                waitingForBaseline ? null : images[499].Id, false, true, 500, outcome);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(state) };
        }

        if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == $"/api/v1{EditorPage}/initialize")
        {
            Interlocked.Increment(ref initializationCount);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        if (request.Method == HttpMethod.Put
            && request.RequestUri?.AbsolutePath == $"/api/v1{EditorPage}" && request.Content is { } writeBody)
        {
            var placement = (await writeBody.ReadFromJsonAsync<QueueCatalogGalleryPlacementRequest>(cancellationToken)).ShouldNotBeNull();
            writeRequests.Enqueue(placement);
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            response.Headers.Location = new Uri($"/api/v1{EditorPage}?placementSequence=7", UriKind.Relative);
            return response;
        }

        if (request.Method == HttpMethod.Post
            && request.RequestUri?.AbsolutePath == $"/api/v1{EditorPage}/options"
            && request.Content is { } body)
        {
            var options = (await body.ReadFromJsonAsync<CatalogGalleryPhotoOptionsRequest>(cancellationToken)).ShouldNotBeNull();
            choiceRequests.Enqueue(options);
            var matching = images.Where(image => options.IncludedPhotoIds?.Contains(image.Id) != false
                && (string.IsNullOrEmpty(options.Query) || image.Label.Contains(options.Query, StringComparison.OrdinalIgnoreCase))).ToArray();
            var slice = new CatalogGalleryPhotoOptionsDto([.. matching.Take(options.Limit)], matching.Length);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(slice) };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
