using System.Net;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

/// <summary>Supplies test-owned gallery state and controllable editor responses.</summary>
internal sealed class FakeGalleryEditorApiClient : ICatalogGalleryEditorApiClient
{
    /// <summary>Gets an editable gallery that still uses legacy placements.</summary>
    public static CatalogGalleryEditorStateDto LegacyState
    {
        get;
    } = new(
        StreamVersion: 3, ProjectedStreamVersion: 3, AcceptedPlacementSequence: 0, ResolvedPlacementSequence: 0,
        PhotoIds: null, CoverId: null, IsRetired: false, CanEditGallery: true, MaximumPhotoCount: 500, Outcome: null);

    /// <summary>Gets or sets the response for each editor state request.</summary>
    public Func<Guid, Task<HttpOutcome<CatalogGalleryEditorStateDto>>> StateHandler
    {
        get; set;
    } =
        static _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, LegacyState));

    /// <summary>Gets or sets the response for initialization requests.</summary>
    public Func<Guid, Task<HttpOutcome>> InitializeHandler
    {
        get; set;
    } =
        static _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.NoContent, location: null));

    /// <summary>Gets or sets the response for queued placement requests.</summary>
    public Func<Guid, Task<HttpOutcome>> PlacementHandler
    {
        get; set;
    } =
        static _ => Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.Accepted, location: null));

    /// <summary>Gets the ordered tour and placement identifiers for state reads.</summary>
    public List<(Guid TourId, long? PlacementSequence)> StateReads { get; } = [];

    /// <summary>Gets the placement requests sent by the editor.</summary>
    public List<QueueCatalogGalleryPlacementRequest> PlacementRequests { get; } = [];

    /// <summary>Gets or sets the response for photo option requests.</summary>
    public Func<CatalogGalleryPhotoOptionsRequest, Task<HttpOutcome<CatalogGalleryPhotoOptionsDto>>> PhotoHandler { get; set; } = CreatePhotoPage;

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogGalleryEditorStateDto>> GetState(Guid tourId, long? placementSequence, CancellationToken ct)
    {
        StateReads.Add((tourId, placementSequence));
        return StateHandler(tourId);
    }

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogGalleryPhotoOptionsDto>> SearchPhotos(Guid tourId, CatalogGalleryPhotoOptionsRequest request, CancellationToken ct) =>
        PhotoHandler(request);

    /// <summary>Builds a successful page with one labeled option for each requested photo.</summary>
    public static Task<HttpOutcome<CatalogGalleryPhotoOptionsDto>> CreatePhotoPage(CatalogGalleryPhotoOptionsRequest request)
    {
        var photos = (request.IncludedPhotoIds ?? []).Select(id => new CatalogGalleryPhotoOptionDto(id, "Gallery photo", null, null)).ToArray();
        return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, new CatalogGalleryPhotoOptionsDto(photos, photos.Length)));
    }

    /// <inheritdoc />
    public Task<HttpOutcome> Initialize(Guid tourId, CancellationToken ct) => InitializeHandler(tourId);

    /// <inheritdoc />
    public Task<HttpOutcome> QueuePlacement(Guid tourId, QueueCatalogGalleryPlacementRequest request, CancellationToken ct)
    {
        PlacementRequests.Add(request);
        return PlacementHandler(tourId);
    }

    /// <summary>Releases a delayed editor response after the intervening steps finish.</summary>
    public static async Task ReleaseAfter(Func<Task> whilePending, Action release)
    {
        try
        {
            await whilePending();
        }
        finally
        {
            release();
        }
    }
}
