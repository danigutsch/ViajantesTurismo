using Microsoft.EntityFrameworkCore;
using SharedKernel.EventSourcing;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies transaction, order and replay boundaries of Catalog placement intents.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryIntentPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task An_out_of_order_wake_drains_committed_intents_once_even_when_inline_projection_fails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes", "Andes", Guid.CreateVersion7());
        tour.ChangePresentation(tour.Title, tour.Slug, "Ride the Andes.", "", "", "", "").IsSuccess.ShouldBeTrue();
        tour.Publish().IsSuccess.ShouldBeTrue();
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        await scenario.Events.Append(stream, ExpectedStreamRevision.NoStream, tour.GetUncommittedEvents(), ct);
        var initialCount = await scenario.Runner.Project(ct);
        initialCount.ShouldBe(3);
        var first = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
        var second = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "second.jpg", "second.jpg", "sha256:second", "Second", 1, false);
        await scenario.Images.Upsert(first, ct);
        await scenario.Images.Upsert(second, ct);
        var relational = scenario.Database.Database;
        await relational.ExecuteSqlRawAsync("""ALTER TABLE messaging.outbox_messages ADD CONSTRAINT "RejectGalleryWake" CHECK (false)""", ct);

        // Act
        var denied = await scenario.Writer.QueueGallery(tour.Id, [first.Id], first.Id, 3, 0, canPublish: false, ct);
        Func<Task> rejectWake = async () => await scenario.Writer.QueueGallery(tour.Id, [first.Id], first.Id, 3, 0, canPublish: true, ct);
        await rejectWake.ShouldThrow<DbUpdateException>();
        scenario.Database.ChangeTracker.Clear();
        var rolledBack = await scenario.Intents.GetIntent(tour.Id, 1, ct);
        var outboxAfterRollback = await scenario.CountOutbox(ct);
        await relational.ExecuteSqlRawAsync("""ALTER TABLE messaging.outbox_messages DROP CONSTRAINT "RejectGalleryWake" """, ct);

        var firstQueued = await scenario.Writer.QueueGallery(tour.Id, [second.Id, first.Id], second.Id, 3, 0, canPublish: true, ct);
        var secondSequence = await scenario.Intents.Enqueue(tour.Id, [first.Id], first.Id, canPublish: true, ct);
        var wakes = await scenario.CountOutbox(ct);
        var firstIntent = await scenario.Intents.GetIntent(tour.Id, firstQueued.Value, ct);
        var secondIntent = await scenario.Intents.GetIntent(tour.Id, secondSequence, ct);
        var wakeOne = await scenario.GetWake(firstIntent.ShouldNotBeNull().Id, ct);
        var wakeTwo = await scenario.GetWake(secondIntent.ShouldNotBeNull().Id, ct);
        await relational.ExecuteSqlRawAsync(
            """ALTER TABLE "CatalogTourReadModels" ADD CONSTRAINT "RejectGalleryProjection" CHECK ("GalleryPhotoIds" IS NULL)""", ct);
        Func<Task> wakeSecond = async () => await scenario.Handler.Handle(wakeTwo, ct);
        await wakeSecond.ShouldThrow<CatalogTourProjectionPendingException>();
        var firstAppend = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(3), ct);

        await relational.ExecuteSqlRawAsync("""ALTER TABLE "CatalogTourReadModels" DROP CONSTRAINT "RejectGalleryProjection" """, ct);
        scenario.Database.ChangeTracker.Clear();
        var replayed = await scenario.Runner.Project(ct);
        scenario.AdvanceBeyondInboxLease();
        await scenario.Handler.Handle(wakeTwo, ct);
        await scenario.Handler.Handle(wakeOne, ct);
        await scenario.Handler.Handle(wakeTwo, ct);
        var applied = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(3), ct);
        var projected = await scenario.Tours.GetTour(tour.Id, ct);

        // Assert
        denied.IsFailure.ShouldBeTrue();
        rolledBack.ShouldBeNull();
        outboxAfterRollback.ShouldBe(0);
        firstQueued.Value.ShouldBe(1);
        secondSequence.ShouldBe(2);
        wakes.ShouldBe(2);
        var firstEvent = firstAppend.ShouldHaveSingleItem();
        firstEvent.Data.ShouldBeOfType<CatalogTourGalleryChanged>().IntentSequence.ShouldBe(1);
        firstEvent.Data.ShouldBeOfType<CatalogTourGalleryChanged>().IntentId.ShouldBe(wakeOne.EventId);
        replayed.ShouldBe(1);
        var committed = applied.OrderBy(envelope => envelope.Revision.Value).ToArray();
        committed.Length.ShouldBe(2);
        committed[0].EventId.ShouldBe(firstEvent.EventId);
        committed[1].Data.ShouldBeOfType<CatalogTourGalleryChanged>().IntentSequence.ShouldBe(2);
        var row = projected.ShouldNotBeNull();
        row.StreamVersion.ShouldBe(5);
        row.GalleryPhotoIds.ShouldNotBeNull().ShouldBe([first.Id]);
        row.GalleryCoverId.ShouldBe(first.Id);
        row.IsPubliclyVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task A_draft_only_intent_rejected_after_publication_does_not_block_a_later_authorized_intent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "andes", "Andes", Guid.CreateVersion7());
        tour.ChangePresentation(tour.Title, tour.Slug, "Ride the Andes.", "", "", "", "").IsSuccess.ShouldBeTrue();
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        await scenario.Events.Append(stream, ExpectedStreamRevision.NoStream, tour.GetUncommittedEvents(), ct);
        await scenario.Runner.Project(ct);
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
        await scenario.Images.Upsert(photo, ct);
        var accepted = await scenario.Writer.QueueGallery(tour.Id, [photo.Id], photo.Id, 2, 0, canPublish: false, ct);
        var intent = await scenario.Intents.GetIntent(tour.Id, accepted.Value, ct);
        var wake = await scenario.GetWake(intent.ShouldNotBeNull().Id, ct);
        await scenario.Writer.Publish(tour.Id, 2, ct);
        var laterPhoto = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "second.jpg", "second.jpg", "sha256:second", "Second", 1, false);
        await scenario.Images.Upsert(laterPhoto, ct);
        var laterSequence = await scenario.Intents.Enqueue(tour.Id, [laterPhoto.Id], laterPhoto.Id, canPublish: true, ct);
        var laterIntent = await scenario.Intents.GetIntent(tour.Id, laterSequence, ct);
        var laterWake = await scenario.GetWake(laterIntent.ShouldNotBeNull().Id, ct);
        await scenario.Runner.Project(ct);
        var relational = scenario.Database.Database;
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE "CatalogTourReadModels" ADD CONSTRAINT "RejectOutcomeProjection" CHECK ("StreamVersion" <> 4)
            """, ct);

        // Act
        Func<Task> consume = async () => await scenario.Handler.Handle(laterWake, ct);
        await consume.ShouldThrow<CatalogTourProjectionPendingException>();
        var committedOutcome = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(3), ct);
        var editor = await scenario.Writer.GetGalleryEditorState(tour.Id, accepted.Value, canPublish: false, ct);
        await relational.ExecuteSqlRawAsync("""
            ALTER TABLE "CatalogTourReadModels" DROP CONSTRAINT "RejectOutcomeProjection"
            """, ct);
        var replayed = await scenario.Runner.Project(ct);
        var afterOutcome = await scenario.Tours.GetTour(tour.Id, ct);
        scenario.AdvanceBeyondInboxLease();
        await scenario.Handler.Handle(laterWake, ct);
        await scenario.Handler.Handle(wake, ct);
        await scenario.Handler.Handle(laterWake, ct);
        var events = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(2), ct);
        var projected = await scenario.Tours.GetTour(tour.Id, ct);

        // Assert
        accepted.IsSuccess.ShouldBeTrue();
        events.Count.ShouldBe(3);
        var rejected = committedOutcome.ShouldHaveSingleItem();
        var recorded = rejected.Data.ShouldBeOfType<CatalogTourGalleryIntentRejected>();
        recorded.IntentId.ShouldBe(wake.EventId);
        recorded.IntentSequence.ShouldBe(1);
        editor.Value.StreamVersion.ShouldBe(4);
        editor.Value.ProjectedStreamVersion.ShouldBe(3);
        editor.Value.AcceptedPlacementSequence.ShouldBe(2);
        editor.Value.ResolvedPlacementSequence.ShouldBe(1);
        editor.Value.Outcome.ShouldNotBeNull().Status.ShouldBe(CatalogGalleryPlacementStatusDto.RejectedAfterPublication);
        editor.Value.Outcome.IntentId.ShouldBe(wake.EventId);
        var outcomes = events.Where(envelope => envelope.Data is CatalogTourGalleryIntentRejected);
        outcomes.ShouldHaveSingleItem().EventId.ShouldBe(rejected.EventId);
        replayed.ShouldBe(1);
        afterOutcome.ShouldNotBeNull().StreamVersion.ShouldBe(4);
        afterOutcome.GalleryPhotoIds.ShouldBeNull();
        afterOutcome.IsPubliclyVisible.ShouldBeTrue();
        var change = events.Select(envelope => envelope.Data).OfType<CatalogTourGalleryChanged>().ShouldHaveSingleItem();
        change.IntentSequence.ShouldBe(laterSequence);
        var row = projected.ShouldNotBeNull();
        row.IsPubliclyVisible.ShouldBeTrue();
        row.GalleryPhotoIds.ShouldNotBeNull().ShouldBe([laterPhoto.Id]);
    }
}
