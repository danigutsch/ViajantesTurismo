using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedKernel.EventSourcing;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies transaction, order and replay boundaries of Catalog placement intents.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryIntentPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task An_accepted_placement_keeps_its_identity_across_independent_catalog_scopes()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        Guid targetTourId;
        Guid otherTourId;
        long targetSequence;
        long otherSequence;
        await using (var writer = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct))
        {
            var targetTour = await writer.SeedDraft(authoredGallery: false, ct);
            var otherTour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "coast", "Coast", Guid.CreateVersion7());
            var otherStream = CatalogTourStreamIds.FromAdminTourId(otherTour.AdminTourId);
            await writer.Events.Append(otherStream, ExpectedStreamRevision.NoStream, otherTour.GetUncommittedEvents(), ct);
            await writer.Runner.Project(ct);
            targetTourId = targetTour.Id;
            otherTourId = otherTour.Id;
            var targetPhoto = PublicMediaImageTestFactory.CreateReadyImage(targetTourId, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
            var otherPhoto = PublicMediaImageTestFactory.CreateReadyImage(otherTourId, "second.jpg", "second.jpg", "sha256:second", "Second", 0, true);
            await writer.Images.Upsert(targetPhoto, ct);
            await writer.Images.Upsert(otherPhoto, ct);
            var target = await writer.Writer.QueueGallery(targetTourId, [targetPhoto.Id], targetPhoto.Id, 1, 0, canPublish: false, ct);
            var other = await writer.Writer.QueueGallery(otherTourId, [otherPhoto.Id], otherPhoto.Id, 1, 0, canPublish: false, ct);
            targetSequence = target.Value;
            otherSequence = other.Value;
        }

        // Act
        CatalogGalleryEditorStateDto pending;
        CatalogGalleryPlacementQueuedIntegrationEvent wake;
        await using (var consumer = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct))
        {
            var state = await consumer.Writer.GetGalleryEditorState(targetTourId, targetSequence, canPublish: false, ct);
            pending = state.Value;
            var pendingOutcome = pending.Outcome.ShouldNotBeNull();
            wake = await consumer.GetWake(pendingOutcome.IntentId, ct);
            await consumer.Handler.Handle(wake, ct);
        }

        await using var reader = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var targetState = await reader.Writer.GetGalleryEditorState(targetTourId, targetSequence, canPublish: false, ct);
        var otherState = await reader.Writer.GetGalleryEditorState(otherTourId, otherSequence, canPublish: false, ct);
        var missing = await reader.Writer.GetGalleryEditorState(otherTourId, otherSequence + 1, canPublish: false, ct);
        var targetStatus = await reader.GalleryOperations.Read(targetTourId, targetSequence, ct);
        var targetEvidence = targetStatus.ShouldNotBeNull();
        var terminalEventId = targetEvidence.TerminalEventId.ShouldNotBeNull();
        var otherWakeId = otherState.Value.Outcome.ShouldNotBeNull().IntentId;
        var rewired = await reader.Database.Database.ExecuteSqlAsync($"""
            UPDATE event_sourcing.events SET causation_event_id = {otherWakeId} WHERE event_id = {terminalEventId}
            """, ct);
        var disjointEvidence = await reader.GalleryOperations.Read(targetTourId, targetSequence, ct);

        // Assert
        targetSequence.ShouldBe(otherSequence);
        var before = pending.Outcome.ShouldNotBeNull();
        before.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        pending.AcceptedPlacementSequence.ShouldBe(targetSequence);
        pending.ResolvedPlacementSequence.ShouldBe(0);
        wake.CatalogTourId.ShouldBe(targetTourId);
        wake.EventId.ShouldBe(before.IntentId);
        wake.Sequence.ShouldBe(targetSequence);
        var applied = targetState.Value.Outcome.ShouldNotBeNull();
        applied.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        applied.IntentId.ShouldBe(before.IntentId);
        applied.Sequence.ShouldBe(targetSequence);
        targetState.Value.ResolvedPlacementSequence.ShouldBe(targetSequence);
        var unchanged = otherState.Value.Outcome.ShouldNotBeNull();
        unchanged.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        unchanged.IntentId.ShouldNotBe(before.IntentId);
        unchanged.Sequence.ShouldBe(otherSequence);
        otherState.Value.ResolvedPlacementSequence.ShouldBe(0);
        missing.IsFailure.ShouldBeTrue();
        var targetJson = JsonSerializer.SerializeToElement(targetEvidence, JsonSerializerOptions.Web);
        targetJson.GetProperty("triggeringWakeEventId").GetGuid().ShouldBe(wake.EventId);
        JsonElement.DeepEquals(targetJson.GetProperty("triggeringWake"), targetJson.GetProperty("originalWake")).ShouldBeTrue();
        rewired.ShouldBe(1);
        disjointEvidence.ShouldNotBeNull().CausationEventId.ShouldBe(otherWakeId);
        var disjointJson = JsonSerializer.SerializeToElement(disjointEvidence, JsonSerializerOptions.Web);
        disjointJson.TryGetProperty("triggeringWake", out _).ShouldBeFalse();
        disjointJson.TryGetProperty("triggeringWakeEventId", out _).ShouldBeFalse();
    }

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
        var publishedAt = DateTimeOffset.FromUnixTimeMilliseconds(wakeTwo.OccurredAt.ToUnixTimeMilliseconds()).AddMinutes(1);
        var wakeTwoId = wakeTwo.EventId.ToString("D");
        var published = await relational.ExecuteSqlAsync($"""
            UPDATE messaging.outbox_messages SET "PublishedAt" = {publishedAt} WHERE "EventId" = {wakeTwoId}
            """, ct);
        await relational.ExecuteSqlRawAsync(
            """ALTER TABLE "CatalogTourReadModels" ADD CONSTRAINT "RejectGalleryProjection" CHECK ("GalleryPhotoIds" IS NULL)""", ct);
        Func<Task> wakeSecond = async () => await scenario.Handler.Handle(wakeTwo, ct);
        await wakeSecond.ShouldThrow<CatalogTourProjectionPendingException>();
        var firstAppend = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(3), ct);
        var firstStatus = await scenario.GalleryOperations.Read(tour.Id, firstQueued.Value, ct);

        await relational.ExecuteSqlRawAsync("""ALTER TABLE "CatalogTourReadModels" DROP CONSTRAINT "RejectGalleryProjection" """, ct);
        scenario.Database.ChangeTracker.Clear();
        var replayed = await scenario.Runner.Project(ct);
        scenario.AdvanceBeyondInboxLease();
        await scenario.Handler.Handle(wakeTwo, ct);
        await scenario.Handler.Handle(wakeOne, ct);
        await scenario.Handler.Handle(wakeTwo, ct);
        var applied = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(3), ct);
        var projected = await scenario.Tours.GetTour(tour.Id, ct);
        var secondStatus = await scenario.GalleryOperations.Read(tour.Id, secondSequence, ct);
        var mismatched = await relational.ExecuteSqlAsync($"""
            UPDATE messaging.outbox_messages SET "EventType" = 'fixture.unrelated' WHERE "EventId" = {wakeTwoId}
            """, ct);
        var mismatchedStatus = await scenario.GalleryOperations.Read(tour.Id, firstQueued.Value, ct);
        var removed = await relational.ExecuteSqlAsync($"""
            DELETE FROM messaging.outbox_messages WHERE "EventId" = {wakeTwoId}
            """, ct);
        var afterRemoval = await scenario.GalleryOperations.Read(tour.Id, firstQueued.Value, ct);
        var retainedDuplicates = await scenario.ReadInboxDuplicates(wakeTwo.EventId, ct);

        // Assert
        denied.IsFailure.ShouldBeTrue();
        rolledBack.ShouldBeNull();
        outboxAfterRollback.ShouldBe(0);
        firstQueued.Value.ShouldBe(1);
        secondSequence.ShouldBe(2);
        wakes.ShouldBe(2);
        published.ShouldBe(1);
        var firstEvent = firstAppend.ShouldHaveSingleItem();
        firstEvent.Data.ShouldBeOfType<CatalogTourGalleryChanged>().IntentSequence.ShouldBe(1);
        firstEvent.Data.ShouldBeOfType<CatalogTourGalleryChanged>().IntentId.ShouldBe(wakeOne.EventId);
        firstEvent.CausationEventId.ShouldBe(wakeTwo.EventId);
        var firstObserved = firstStatus.ShouldNotBeNull();
        firstObserved.IntentId.ShouldBe(wakeOne.EventId);
        firstObserved.TerminalEventId.ShouldBe(firstEvent.EventId);
        wakeTwo.EventId.ShouldNotBe(firstObserved.IntentId);
        JsonSerializer.SerializeToElement(firstObserved, JsonSerializerOptions.Web)
            .GetProperty("causationEventId").GetGuid().ShouldBe(wakeTwo.EventId);
        var firstEvidence = JsonSerializer.SerializeToElement(firstObserved, JsonSerializerOptions.Web);
        firstEvidence.GetProperty("originalWake").GetProperty("publishedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        firstEvidence.GetProperty("triggeringWake").GetProperty("publishedAt").GetDateTimeOffset().ShouldBe(publishedAt);
        mismatched.ShouldBe(1);
        mismatchedStatus.ShouldNotBeNull().CausationEventId.ShouldBe(wakeTwo.EventId);
        var mismatchedJson = JsonSerializer.SerializeToElement(mismatchedStatus, JsonSerializerOptions.Web);
        mismatchedJson.TryGetProperty("triggeringWake", out _).ShouldBeFalse();
        mismatchedJson.TryGetProperty("triggeringWakeEventId", out _).ShouldBeFalse();
        removed.ShouldBe(1);
        afterRemoval.ShouldNotBeNull().CausationEventId.ShouldBe(wakeTwo.EventId);
        afterRemoval.OriginalWake.ShouldNotBeNull();
        var afterRemovalJson = JsonSerializer.SerializeToElement(afterRemoval, JsonSerializerOptions.Web);
        afterRemovalJson.TryGetProperty("triggeringWake", out _).ShouldBeFalse();
        afterRemovalJson.GetProperty("triggeringWakeEventId").GetGuid().ShouldBe(wakeTwo.EventId);
        retainedDuplicates.Rows.ShouldHaveSingleItem().EventId.ShouldBe(wakeTwo.EventId);
        replayed.ShouldBe(1);
        var committed = applied.OrderBy(envelope => envelope.Revision.Value).ToArray();
        committed.Length.ShouldBe(2);
        committed[0].EventId.ShouldBe(firstEvent.EventId);
        committed[0].CausationEventId.ShouldBe(wakeTwo.EventId);
        committed[1].Data.ShouldBeOfType<CatalogTourGalleryChanged>().IntentSequence.ShouldBe(2);
        committed[1].CausationEventId.ShouldBe(wakeTwo.EventId);
        var secondObserved = secondStatus.ShouldNotBeNull();
        secondObserved.TerminalEventId.ShouldBe(committed[1].EventId);
        JsonSerializer.SerializeToElement(secondObserved, JsonSerializerOptions.Web)
            .GetProperty("causationEventId").GetGuid().ShouldBe(wakeTwo.EventId);
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
        rejected.CausationEventId.ShouldBe(laterWake.EventId);
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
