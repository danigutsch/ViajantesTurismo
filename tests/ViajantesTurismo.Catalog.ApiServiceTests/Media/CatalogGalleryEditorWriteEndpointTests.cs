using PandaBear.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

/// <summary>Verifies that Catalog accepts a scoped editor placement or returns the existing boundary failure.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class CatalogGalleryEditorWriteEndpointTests
{
    [Fact]
    public async Task A_draft_editor_can_queue_one_snapshot_and_follow_its_outcome_location()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var editor = host.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(editor, "Operator");
        Guid[] photoIds = [scenario.SecondPhotoId, scenario.FirstPhotoId];
        var request = new
        {
            StreamVersion = 1L,
            AcceptedPlacementSequence = 0L,
            PhotoIds = photoIds,
            CoverId = scenario.SecondPhotoId
        };
        var staleStream = request with
        {
            StreamVersion = 2L
        };
        var pendingPlacement = request with
        {
            AcceptedPlacementSequence = 1L
        };

        // Act
        using var stale = await editor.PutAsJsonAsync(scenario.EditorPath, staleStream, ct);
        using var accepted = await editor.PutAsJsonAsync(scenario.EditorPath, request, ct);
        using var repeated = await editor.PutAsJsonAsync(scenario.EditorPath, request, ct);
        using var pending = await editor.PutAsJsonAsync(scenario.EditorPath, pendingPlacement, ct);

        // Assert
        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        accepted.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        repeated.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        pending.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var location = accepted.Headers.Location.ShouldNotBeNull();
        location.ToString().ShouldBe($"{scenario.EditorPath}?placementSequence=1");

        // Act
        using var outcomeResponse = await editor.GetAsync(location, ct);
        var outcome = await outcomeResponse.Content.ReadFromJsonAsync<CatalogGalleryEditorStateDto>(ct);
        var stored = await scenario.Journal.GetIntent(scenario.TourId, 1, ct);
        var highWater = await scenario.Journal.GetHighWatermark(scenario.TourId, ct);

        // Assert
        outcomeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        outcome.ShouldNotBeNull().Outcome.ShouldNotBeNull().Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        outcome.PhotoIds.ShouldBeNull();
        stored.ShouldNotBeNull().PhotoIds.ShouldBe(photoIds);
        stored.CoverId.ShouldBe(scenario.SecondPhotoId);
        stored.CanPublish.ShouldBeFalse();
        highWater.ShouldBe(1);
    }

    [Fact]
    public async Task A_published_gallery_requires_the_existing_publication_permission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: true, ct);
        await using var host = scenario.CreateHost();
        using var editor = host.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(editor, "Operator");
        using var publisher = host.CreateClient();
        using var anonymous = host.CreateClient();
        anonymous.DefaultRequestHeaders.Authorization = null;
        Guid[] photoIds = [scenario.SecondPhotoId];
        var request = new
        {
            StreamVersion = 3L,
            AcceptedPlacementSequence = 0L,
            PhotoIds = photoIds,
            CoverId = scenario.SecondPhotoId
        };

        // Act
        using var unauthenticated = await anonymous.PutAsJsonAsync(scenario.EditorPath, request, ct);
        using var denied = await editor.PutAsJsonAsync(scenario.EditorPath, request, ct);
        var beforeAcceptance = await scenario.Journal.GetHighWatermark(scenario.TourId, ct);
        using var accepted = await publisher.PutAsJsonAsync(scenario.EditorPath, request, ct);
        var stored = await scenario.Journal.GetIntent(scenario.TourId, 1, ct);

        // Assert
        unauthenticated.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        beforeAcceptance.ShouldBe(0);
        accepted.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        stored.ShouldNotBeNull().CanPublish.ShouldBeTrue();
    }

    [Fact]
    public async Task Missing_revision_and_foreign_cover_are_rejected_before_queuing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var editor = host.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(editor, "Operator");
        Guid[] photoIds = [scenario.FirstPhotoId];
        var noPlacementRevision = new
        {
            StreamVersion = 1L,
            PhotoIds = photoIds
        };
        var foreignCover = new
        {
            StreamVersion = 1L,
            AcceptedPlacementSequence = 0L,
            PhotoIds = photoIds,
            CoverId = Guid.CreateVersion7()
        };

        // Act
        using var missing = await editor.PutAsJsonAsync(scenario.EditorPath, noPlacementRevision, ct);
        using var unauthorizedCover = await editor.PutAsJsonAsync(scenario.EditorPath, foreignCover, ct);
        using var emptyTour = await editor.PutAsJsonAsync(new Uri($"/api/v1/catalog/tours/{Guid.Empty}/gallery-editor", UriKind.Relative), foreignCover, ct);
        using var unknownTour = await editor.PutAsJsonAsync(new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}/gallery-editor", UriKind.Relative), foreignCover, ct);
        var highWater = await scenario.Journal.GetHighWatermark(scenario.TourId, ct);

        // Assert
        missing.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        unauthorizedCover.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        emptyTour.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        unknownTour.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        highWater.ShouldBe(0);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, -1, false)]
    [InlineData(1, 0, true)]
    public async Task Invalid_editor_tokens_or_a_null_selection_never_enter_the_journal(
        long streamVersion, long acceptedPlacementSequence, bool nullSelection)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var editor = host.CreateClient();
        var request = new
        {
            StreamVersion = streamVersion,
            AcceptedPlacementSequence = acceptedPlacementSequence,
            PhotoIds = nullSelection ? null : Array.Empty<Guid>()
        };

        // Act
        using var response = await editor.PutAsJsonAsync(scenario.EditorPath, request, ct);
        var highWater = await scenario.Journal.GetHighWatermark(scenario.TourId, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        highWater.ShouldBe(0);
    }
}
