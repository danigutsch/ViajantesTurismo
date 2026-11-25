using ImageMagick;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Media;

/// <summary>Exercises the ordered placement producer in the real guarded upload transaction.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class GalleryUploadPlacementPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Accepted_uploads_journal_once_in_order_and_keep_original_scan_and_sanitized_storage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: true, ct);
        var cover = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "cover.jpg", "cover-640.jpg", "sha256:cover", "Cover", 0, true);
        await scenario.Images.Upsert(cover, ct);
        var authored = await scenario.Writer.ChangeGallery(tour.Id, [cover.Id], cover.Id, 2, canPublish: false, ct);
        authored.IsSuccess.ShouldBeTrue();
        using var blue = new MagickImage(MagickColors.Blue, 64, 32);
        var cleaned = blue.ToByteArray(MagickFormat.Jpeg);
        blue.Comment = "Test metadata stripped on storage";
        var original = blue.ToByteArray(MagickFormat.Jpeg);
        using var firstContent = new MemoryStream(original);
        using var retryContent = new MemoryStream(original);
        using var secondContent = new MemoryStream(original);
        var first = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), firstContent, "boat.jpg", "image/jpeg", original.Length, "First caption", []);
        var retry = first with
        {
            MediaImageId = Guid.CreateVersion7(),
            Content = retryContent
        };
        var second = first with
        {
            MediaImageId = Guid.CreateVersion7(),
            Content = secondContent,
            AltText = "Second caption"
        };
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstScanner = new ExactGalleryUploadScanner(original, releaseFirst.Task);
        var scanner = new ExactGalleryUploadScanner(original);
        var objects = MediaObjectStoreTestFactory.Create();
        using var observation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        observation.CancelAfter(TimeSpan.FromSeconds(30));

        // Act
        var pendingFirst = scenario.AcceptUpload(tour.Id, first, firstScanner, objects, observation.Token).AsTask();
        await firstScanner.Started.WaitAsync(observation.Token);
        var pendingSecond = scenario.AcceptUpload(tour.Id, second, scanner, objects, observation.Token).AsTask();
        await GalleryPlacementPostgreSqlScenario.ReleaseWhenMutationWaits(database.ConnectionString, releaseFirst, observation.Token);

        var accepted = await pendingFirst;
        var another = await pendingSecond;
        var duplicate = await scenario.AcceptUpload(tour.Id, retry, scanner, objects, ct);
        var firstPlacement = await scenario.Intents.GetIntent(tour.Id, 1, ct);
        var secondPlacement = await scenario.Intents.GetIntent(tour.Id, 2, ct);
        var thirdPlacement = await scenario.Intents.GetIntent(tour.Id, 3, ct);
        var wakes = await scenario.CountOutbox(ct);
        var lastWake = await scenario.GetWake(secondPlacement.ShouldNotBeNull().Id, ct);
        await scenario.Handler.Handle(lastWake, ct);
        var projected = await scenario.Tours.GetTour(tour.Id, ct);

        // Assert
        accepted.IsSuccess.ShouldBeTrue();
        duplicate.Value.Image.Id.ShouldBe(accepted.Value.Image.Id);
        duplicate.Value.OriginalStoredEvent.ShouldBeNull();
        another.IsSuccess.ShouldBeTrue();
        firstPlacement.ShouldNotBeNull().PhotoIds.ShouldBe([first.MediaImageId]);
        secondPlacement.ShouldNotBeNull().PhotoIds.ShouldBe([second.MediaImageId]);
        thirdPlacement.ShouldBeNull();
        wakes.ShouldBe(4L);
        projected.ShouldNotBeNull().GalleryPhotoIds.ShouldNotBeNull().ShouldBe([cover.Id, first.MediaImageId, second.MediaImageId]);
        projected.GalleryCoverId.ShouldBe(cover.Id);
        firstScanner.Scans.ShouldBe(1);
        scanner.Scans.ShouldBe(2);
        objects.Puts.Count.ShouldBe(2);
        foreach (var photo in new[] { accepted.Value.Image, another.Value.Image })
        {
            var stored = objects.FindObject(photo.SourceObjectKey).ShouldNotBeNull();
            stored.Content.Span.SequenceEqual(cleaned).ShouldBeTrue();
            stored.Checksum.ShouldBe(photo.Checksum);
        }
    }

    [Fact]
    public async Task Rejected_placement_wake_rolls_back_image_and_events_and_removes_candidate_object()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: true, ct);
        using var image = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        using var rejectedContent = new MemoryStream(bytes);
        using var retryContent = new MemoryStream(bytes);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), rejectedContent, "boat.jpg", "image/jpeg", bytes.Length, "Blue boat", []);
        var scanner = new ExactGalleryUploadScanner(bytes);
        var objects = MediaObjectStoreTestFactory.Create();
        await scenario.Database.Database.ExecuteSqlRawAsync("""
            ALTER TABLE messaging.outbox_messages ADD CONSTRAINT "RejectUploadedPlacementWake"
            CHECK ("EventType" <> 'catalog.gallery-placement.queued')
            """, ct);

        // Act
        Func<Task> reject = async () => await scenario.AcceptUpload(tour.Id, request, scanner, objects, ct);
        await reject.ShouldThrow<DbUpdateException>();
        var orphanImage = await scenario.Images.GetImage(request.MediaImageId, ct);
        var orphanIntent = await scenario.Intents.GetIntent(tour.Id, 1, ct);
        var orphanEvents = await scenario.CountOutbox(ct);
        var discardedKey = objects.Deletes.ShouldHaveSingleItem();
        var orphanObject = objects.FindObject(discardedKey);
        await scenario.Database.Database.ExecuteSqlRawAsync("""
            ALTER TABLE messaging.outbox_messages DROP CONSTRAINT "RejectUploadedPlacementWake"
            """, ct);
        var retry = request with
        {
            Content = retryContent
        };
        var retried = await scenario.AcceptUpload(tour.Id, retry, scanner, objects, ct);
        var savedIntent = await scenario.Intents.GetIntent(tour.Id, 1, ct);
        var savedEvents = await scenario.CountOutbox(ct);

        // Assert
        orphanImage.ShouldBeNull();
        orphanIntent.ShouldBeNull();
        orphanEvents.ShouldBe(0L);
        orphanObject.ShouldBeNull();
        retried.IsSuccess.ShouldBeTrue();
        savedIntent.ShouldNotBeNull().PhotoIds.ShouldBe([request.MediaImageId]);
        savedEvents.ShouldBe(2L);
        objects.ObjectKeys.ShouldHaveSingleItem().ShouldBe(retried.Value.Image.SourceObjectKey);
    }

    [Fact]
    public async Task A_legacy_tour_retains_its_linked_gallery_until_a_reviewed_baseline_exists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var existing = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "legacy.jpg", "legacy-640.jpg", "sha256:legacy", "Legacy", 0, true);
        await scenario.Images.Upsert(existing, ct);
        using var image = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        using var content = new MemoryStream(bytes);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), content, "boat.jpg", "image/jpeg", bytes.Length, "Blue boat", []);

        // Act
        var accepted = await scenario.AcceptUpload(tour.Id, request, new ExactGalleryUploadScanner(bytes), MediaObjectStoreTestFactory.Create(), ct);
        var intent = await scenario.Intents.GetIntent(tour.Id, 1, ct);
        var wake = await scenario.GetWake(intent.ShouldNotBeNull().Id, ct);
        await scenario.Handler.Handle(wake, ct);
        var projected = await scenario.Tours.GetTour(tour.Id, ct);
        var linked = await scenario.Images.ListByTour(tour.Id, ct);

        // Assert
        accepted.IsSuccess.ShouldBeTrue();
        intent.PhotoIds.ShouldBe([request.MediaImageId]);
        projected.ShouldNotBeNull().GalleryPhotoIds.ShouldBeNull();
        projected.StreamVersion.ShouldBe(1);
        linked.Select(photo => photo.Id).ShouldBe([existing.Id, request.MediaImageId]);
    }
}
