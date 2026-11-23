using Microsoft.EntityFrameworkCore;
using PandaBear.EventSourcing;
using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Exercises editor admission and blocked placement on the real Catalog journal.</summary>
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryEditorRevisionPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task An_editor_snapshot_waits_for_the_current_pending_placement()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
        await scenario.Images.Upsert(photo, ct);
        var accepted = await scenario.Writer.QueueGallery(tour.Id, [photo.Id], photo.Id, 1, 0, canPublish: false, ct);
        var intent = await scenario.Intents.GetIntent(tour.Id, accepted.Value, ct);
        var wake = await scenario.GetWake(intent.ShouldNotBeNull().Id, ct);

        // Act
        var stale = await scenario.Writer.QueueGallery(tour.Id, [], null, 1, 0, canPublish: false, ct);
        var pending = await scenario.Writer.QueueGallery(tour.Id, [], null, 1, 1, canPublish: false, ct);
        var pendingState = await scenario.Writer.GetGalleryEditorState(tour.Id, placementSequence: null, canPublish: false, ct);
        var direct = await scenario.Writer.ChangeGallery(tour.Id, [], null, 1, canPublish: false, ct);
        var through = await scenario.Intents.GetHighWatermark(tour.Id, ct);
        var outbox = await scenario.CountOutbox(ct);
        await scenario.Handler.Handle(wake, ct);
        var afterDrain = await scenario.Writer.QueueGallery(tour.Id, [], null, 2, 1, canPublish: false, ct);
        var appliedState = await scenario.Writer.GetGalleryEditorState(tour.Id, 1, canPublish: false, ct);

        // Assert
        accepted.Value.ShouldBe(1);
        stale.Status.ShouldBe(ResultStatus.Conflict);
        pending.Status.ShouldBe(ResultStatus.Conflict);
        direct.Status.ShouldBe(ResultStatus.Conflict);
        through.ShouldBe(1);
        outbox.ShouldBe(1);
        afterDrain.Value.ShouldBe(2);
        pendingState.Value.AcceptedPlacementSequence.ShouldBe(1);
        pendingState.Value.ResolvedPlacementSequence.ShouldBe(0);
        pendingState.Value.Outcome.ShouldNotBeNull().Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        var applied = appliedState.Value;
        applied.AcceptedPlacementSequence.ShouldBe(2);
        applied.ResolvedPlacementSequence.ShouldBe(1);
        applied.StreamVersion.ShouldBe(2);
        applied.ProjectedStreamVersion.ShouldBe(2);
        applied.Outcome.ShouldNotBeNull().Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        applied.Outcome.IntentId.ShouldBe(wake.EventId);
        applied.PhotoIds.ShouldNotBeNull().ShouldBe([photo.Id]);
        applied.CoverId.ShouldBe(photo.Id);
    }

    [Fact]
    public async Task An_unpublished_tour_waits_for_its_public_projection_before_applying_a_draft_intent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedPublishableDraft(ct);
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
        await scenario.Images.Upsert(photo, ct);
        var accepted = await scenario.Writer.QueueGallery(tour.Id, [photo.Id], photo.Id, 2, 0, canPublish: false, ct);
        var intent = await scenario.Intents.GetIntent(tour.Id, accepted.Value, ct);
        var wake = await scenario.GetWake(intent.ShouldNotBeNull().Id, ct);
        await scenario.Writer.Publish(tour.Id, 2, ct);
        await scenario.Runner.Project(ct);
        var relational = scenario.Database.Database;
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE "CatalogTourReadModels" ADD CONSTRAINT "RejectUnpublishProjection" CHECK ("IsPublished")
            """, ct);
        Func<Task> unpublish = async () => await scenario.Writer.Unpublish(tour.Id, 3, ct);
        await unpublish.ShouldThrow<CatalogTourProjectionPendingException>();
        scenario.Database.ChangeTracker.Clear();

        // Act
        Func<Task> consume = async () => await scenario.Handler.Handle(wake, ct);
        await consume.ShouldThrow<CatalogTourProjectionPendingException>();
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE "CatalogTourReadModels" DROP CONSTRAINT "RejectUnpublishProjection"
            """, ct);
        var replayed = await scenario.Runner.Project(ct);
        scenario.AdvanceBeyondInboxLease();
        await scenario.Handler.Handle(wake, ct);
        var events = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(3), ct);
        var projected = await scenario.Tours.GetTour(tour.Id, ct);

        // Assert
        replayed.ShouldBe(1);
        events.Count.ShouldBe(2);
        var applied = events.Select(envelope => envelope.Data).OfType<CatalogTourGalleryChanged>().ShouldHaveSingleItem();
        applied.IntentSequence.ShouldBe(1);
        projected.ShouldNotBeNull().GalleryPhotoIds.ShouldNotBeNull().ShouldBe([photo.Id]);
        projected.IsPubliclyVisible.ShouldBeFalse();
    }

    [Fact]
    public async Task A_retirement_fence_completes_old_wakeups_without_appending_to_the_closed_stream()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var accepted = await scenario.Writer.QueueGallery(tour.Id, [], null, 1, 0, canPublish: false, ct);
        var intent = await scenario.Intents.GetIntent(tour.Id, accepted.Value, ct);
        var wake = await scenario.GetWake(intent.ShouldNotBeNull().Id, ct);
        await scenario.Retire(tour, ct);

        // Act
        await scenario.Handler.Handle(wake, ct);
        await scenario.Handler.Handle(wake, ct);
        var events = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(1), ct);
        var projected = await scenario.Tours.GetTour(tour.Id, ct);
        var editor = await scenario.Writer.GetGalleryEditorState(tour.Id, accepted.Value, canPublish: false, ct);

        // Assert
        events.ShouldHaveSingleItem().Data.ShouldBeOfType<CatalogTourRetired>();
        projected.ShouldNotBeNull().GalleryPhotoIds.ShouldBeNull();
        projected.IsPubliclyVisible.ShouldBeFalse();
        editor.Value.IsRetired.ShouldBeTrue();
        editor.Value.Outcome.ShouldNotBeNull().Status.ShouldBe(CatalogGalleryPlacementStatusDto.ClosedByRetirement);
    }
}
