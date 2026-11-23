using ImageMagick;
using Microsoft.EntityFrameworkCore;
using PandaBear.EventSourcing;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using ViajantesTurismo.HostedTests.Catalog.Infrastructure.Media;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies recovery of the legacy baseline and its separate durable placement wake.</summary>
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryBootstrapPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Draft_initialization_retries_an_already_committed_baseline_until_projection_recovers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var cover = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "cover.jpg", "cover-640.jpg", "sha256:cover", "Cover", 0, true);
        await scenario.Images.Upsert(cover, ct);
        var relational = scenario.Database.Database;
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE "CatalogTourReadModels" ADD CONSTRAINT "WaitForGalleryProjection" CHECK ("GalleryPhotoIds" IS NULL)
            """, ct);

        // Act
        Func<Task> initialize = async () => await scenario.Writer.BootstrapLegacyGallery(tour.Id, canPublish: false, ct);
        await initialize.ShouldThrow<CatalogTourProjectionPendingException>();
        await initialize.ShouldThrow<CatalogTourProjectionPendingException>();
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE "CatalogTourReadModels" DROP CONSTRAINT "WaitForGalleryProjection"
            """, ct);
        scenario.Database.ChangeTracker.Clear();
        await scenario.Runner.Project(ct);
        var repeated = await scenario.Writer.BootstrapLegacyGallery(tour.Id, canPublish: false, ct);
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var events = await scenario.Events.Load(stream, StreamRevision.From(1), ct);

        // Assert
        repeated.IsSuccess.ShouldBeTrue();
        repeated.Value.GalleryPhotoIds.ShouldNotBeNull().ShouldBe([cover.Id]);
        repeated.Value.GalleryCoverId.ShouldBe(cover.Id);
        events.ShouldHaveSingleItem().Data.ShouldBeOfType<CatalogTourGalleryChanged>();
    }

    [Fact]
    public async Task Bootstrap_resumes_a_pending_upload_once_after_projection_and_wake_failures()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var first = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first-640.jpg", "sha256:first", "First", 0, false);
        var cover = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "cover.jpg", "cover-640.jpg", "sha256:cover", "Cover", 1, true);
        await scenario.Images.Upsert(first, ct);
        await scenario.Images.Upsert(cover, ct);
        using var photo = new MagickImage(MagickColors.Blue, 64, 32);
        var bytes = photo.ToByteArray(MagickFormat.Jpeg);
        using var content = new MemoryStream(bytes);
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(), content, "boat.jpg", "image/jpeg", bytes.Length, "Blue boat", []);
        var upload = await scenario.AcceptUpload(tour.Id, request, new ExactGalleryUploadScanner(bytes), MediaObjectStoreTestFactory.Create(), ct);
        upload.IsSuccess.ShouldBeTrue();
        var pending = await scenario.Intents.GetIntent(tour.Id, 1, ct);
        var intent = pending.ShouldNotBeNull();
        var oldWake = await scenario.GetWake(intent.Id, ct);
        await scenario.Handler.Handle(oldWake, ct);
        var update = new CatalogTourPresentationUpdate("Andes", "andes", "Ride the Andes", "", "", "", "");
        var prepared = await scenario.Writer.UpdatePresentation(tour.Id, update, 1, ct);
        prepared.IsSuccess.ShouldBeTrue();
        var published = await scenario.Writer.Publish(tour.Id, 2, ct);
        published.IsSuccess.ShouldBeTrue();
        await scenario.Runner.Project(ct);
        var relational = scenario.Database.Database;
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE "CatalogTourReadModels" ADD CONSTRAINT "RejectBootstrapProjection" CHECK ("GalleryPhotoIds" IS NULL)
            """, ct);

        // Act
        Func<Task> seed = async () => await scenario.Writer.BootstrapLegacyGallery(tour.Id, canPublish: true, ct);
        await seed.ShouldThrow<CatalogTourProjectionPendingException>();
        await seed.ShouldThrow<CatalogTourProjectionPendingException>();
        var committed = await scenario.Events.Load(stream, StreamRevision.From(3), ct);
        var baseline = committed.ShouldHaveSingleItem();
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE "CatalogTourReadModels" DROP CONSTRAINT "RejectBootstrapProjection"
            """, ct);
        scenario.Database.ChangeTracker.Clear();
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE messaging.outbox_messages ADD CONSTRAINT "RejectBootstrapWake" CHECK (false) NOT VALID
            """, ct);
        Func<Task> replay = async () => await scenario.Runner.Project(ct);
        await replay.ShouldThrow<DbUpdateException>();
        var projectedBeforeWake = await scenario.Tours.GetTour(tour.Id, ct);
        var wakesAfterFailure = await scenario.CountOutbox(ct);
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE messaging.outbox_messages DROP CONSTRAINT "RejectBootstrapWake"
            """, ct);
        scenario.Database.ChangeTracker.Clear();
        var recovered = await scenario.Runner.Project(ct);
        var wake = await scenario.GetWake(baseline.EventId, ct);
        var later = await scenario.Intents.Enqueue(tour.Id, [cover.Id, first.Id, request.MediaImageId], cover.Id, canPublish: true, ct);
        await scenario.Handler.Handle(wake, ct);
        await scenario.Handler.Handle(oldWake, ct);
        await scenario.Handler.Handle(wake, ct);
        var repeated = await scenario.Writer.BootstrapLegacyGallery(tour.Id, canPublish: true, ct);
        var events = await scenario.Events.Load(stream, StreamRevision.From(3), ct);
        var actual = await scenario.Tours.GetTour(tour.Id, ct);
        var wakes = await scenario.CountOutbox(ct);
        var bootstrapWakeId = wake.EventId.ToString("D");
        await relational.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM messaging.outbox_messages WHERE "EventId" = {bootstrapWakeId}
            """, ct);
        await scenario.TourProjection.Apply(baseline, ct);
        var wakesAfterRetention = await scenario.CountOutbox(ct);
        Func<Task> replaceReceipt = async () => await scenario.Intents.ResumeBootstrap(tour.Id, wake.Sequence, Guid.CreateVersion7(), ct);
        await replaceReceipt.ShouldThrow<InvalidOperationException>();

        // Assert
        Guid[] photoIds = [first.Id, cover.Id, request.MediaImageId];
        baseline.Data.ShouldBeOfType<CatalogTourGalleryChanged>().LegacyBootstrapThroughSequence.ShouldBe(1);
        baseline.Data.ShouldBeOfType<CatalogTourGalleryChanged>().PhotoIds.ShouldBe(photoIds);
        projectedBeforeWake.ShouldNotBeNull().GalleryPhotoIds.ShouldNotBeNull().ShouldBe(photoIds);
        wakesAfterFailure.ShouldBe(2L);
        recovered.ShouldBe(1);
        wake.EventId.ShouldNotBe(oldWake.EventId);
        wake.Sequence.ShouldBe(1);
        later.ShouldBe(2);
        repeated.IsSuccess.ShouldBeTrue();
        var galleryEvents = events.OrderBy(envelope => envelope.Revision.Value).ToArray();
        galleryEvents.Length.ShouldBe(2);
        galleryEvents[0].EventId.ShouldBe(baseline.EventId);
        var acknowledgement = galleryEvents[1].Data.ShouldBeOfType<CatalogTourGalleryChanged>();
        acknowledgement.IntentId.ShouldBe(intent.Id);
        acknowledgement.IntentSequence.ShouldBe(1);
        acknowledgement.PhotoIds.ShouldBe(photoIds);
        actual.ShouldNotBeNull().GalleryPhotoIds.ShouldNotBeNull().ShouldBe(photoIds);
        actual.GalleryCoverId.ShouldBe(cover.Id);
        actual.IsPubliclyVisible.ShouldBeTrue();
        wakes.ShouldBe(4L);
        wakesAfterRetention.ShouldBe(3L);
    }

    [Fact]
    public async Task An_empty_legacy_gallery_gets_one_explicit_baseline_and_no_wake()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);

        // Act
        var missing = await scenario.Writer.BootstrapLegacyGallery(Guid.CreateVersion7(), canPublish: false, ct);
        var first = await scenario.Writer.BootstrapLegacyGallery(tour.Id, canPublish: false, ct);
        var repeat = await scenario.Writer.BootstrapLegacyGallery(tour.Id, canPublish: false, ct);
        await scenario.Runner.Project(ct);
        var events = await scenario.Events.Load(stream, StreamRevision.From(1), ct);
        var wakes = await scenario.CountOutbox(ct);

        // Assert
        missing.Status.ShouldBe(PandaBear.Results.ResultStatus.NotFound);
        first.IsSuccess.ShouldBeTrue();
        repeat.Value.GalleryPhotoIds.ShouldNotBeNull().ShouldBeEmpty();
        repeat.Value.GalleryCoverId.ShouldBeNull();
        var baseline = events.ShouldHaveSingleItem().Data.ShouldBeOfType<CatalogTourGalleryChanged>();
        baseline.LegacyBootstrapThroughSequence.ShouldBe(0);
        baseline.IntentSequence.ShouldBeNull();
        wakes.ShouldBe(0L);
    }
}
