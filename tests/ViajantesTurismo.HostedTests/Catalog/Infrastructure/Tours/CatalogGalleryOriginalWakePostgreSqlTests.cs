using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.EventSourcing;
using SharedKernel.Idempotency;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies the original Gallery wake and committed status through their real Catalog stores.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryOriginalWakePostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Original_wake_metadata_remains_correlated_after_a_retry_and_publication()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await using var writer = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        await CatalogGalleryProcessStatusIndexes.EnsureIndexes(dataSource, ct);
        var firstTour = await writer.SeedDraft(authoredGallery: false, ct);
        var secondTour = await writer.SeedDraft("alps", "Alps", authoredGallery: false, ct);
        var (firstIntentId, firstSequence) = await writer.QueuePhoto(firstTour.Id, ct);
        var (secondIntentId, secondSequence) = await writer.QueuePhoto(secondTour.Id, ct);
        var eventId = firstIntentId.ToString("D");
        var eventType = CatalogGalleryPlacementQueuedIntegrationEvent.EventType;
        var eventVersion = CatalogGalleryPlacementQueuedIntegrationEvent.EventVersion;
        var inboxScope = IdempotencyScope.From($"catalog.integration-event.{eventType}.v{eventVersion}");
        var operation = new IdempotencyOperation(inboxScope, IdempotencyKey.From(firstIntentId.ToString("N")));
        var stream = CatalogTourStreamIds.FromAdminTourId(firstTour.AdminTourId);
        await writer.Events.Append(stream, ExpectedStreamRevision.From(StreamRevision.From(1)),
            [new CatalogTourPresentationChanged(firstTour.Id, "Andes north", firstTour.Slug, "Ride the Andes.", "", "", "", "")], ct);

        // Act
        var before = await writer.ProcessStatus.Read(firstTour.Id, firstSequence, ct);
        var operationsBefore = await writer.GalleryOperations.Read(firstTour.Id, firstSequence, ct);
        var unrelated = await writer.ProcessStatus.Read(secondTour.Id, secondSequence, ct);
        var originalWake = await IntegrationEventDeliveryInspection.ReadRetainedOutbox(writer.Database, eventId, ct);
        var beforeInbox = await writer.ReadInbox(operation, ct);
        IntegrationEventOutboxDelivery? failedWake;
        GetCatalogGalleryPlacementStatusDto? operationsRetry;
        DateTimeOffset retryTime;
        TimeSpan inboxLease;
        await using (var failingHost = await CatalogGalleryRelayHost.Start(database.ConnectionString, TimeProvider.System.GetUtcNow(), ct))
        {
            var retry = await CatalogGalleryWakeDelivery.WaitFor(dataSource, firstIntentId, published: false, ct);
            retryTime = retry.NextAttemptAt.ShouldNotBeNull();
            inboxLease = failingHost.InboxLeaseDuration;
            failedWake = await IntegrationEventDeliveryInspection.ReadRetainedOutbox(writer.Database, eventId, ct);
            operationsRetry = await writer.GalleryOperations.Read(firstTour.Id, firstSequence, ct);
        }

        await writer.Runner.Project(ct);
        await using (var recoveredHost = await CatalogGalleryRelayHost.Start(database.ConnectionString, retryTime + inboxLease, ct))
        {
            await CatalogGalleryWakeDelivery.WaitFor(dataSource, firstIntentId, published: true, ct);
        }

        await using var observer = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var published = await IntegrationEventDeliveryInspection.ReadRetainedOutbox(observer.Database, eventId, ct);
        var inbox = await observer.ReadInbox(operation, ct);
        var applied = await observer.ProcessStatus.Read(firstTour.Id, firstSequence, ct);
        var operationsPublished = await observer.GalleryOperations.Read(firstTour.Id, firstSequence, ct);
        var terminal = await observer.Events.Load(stream, StreamRevision.From(2), ct);
        var removed = await observer.Database.Database.ExecuteSqlAsync($"""
            DELETE FROM messaging.outbox_messages WHERE "EventId" = {eventId} AND "PublishedAt" IS NOT NULL
            """, ct);
        var retainedAfterRemoval = await IntegrationEventDeliveryInspection.ReadRetainedOutbox(observer.Database, eventId, ct);
        var statusAfterRemoval = await observer.ProcessStatus.Read(firstTour.Id, firstSequence, ct);
        var operationsAfterRemoval = await observer.GalleryOperations.Read(firstTour.Id, firstSequence, ct);
        var otherTourOperations = await observer.GalleryOperations.Read(secondTour.Id, firstSequence, ct);
        var missingPlacement = await observer.GalleryOperations.Read(firstTour.Id, firstSequence + 1, ct);
        var firstByWake = await observer.GalleryOperations.ReadByOriginalWake(firstIntentId, ct);
        var secondByWake = await observer.GalleryOperations.ReadByOriginalWake(secondIntentId, ct);
        var missingWake = await observer.GalleryOperations.ReadByOriginalWake(Guid.CreateVersion7(), ct);

        // Assert
        firstSequence.ShouldBe(secondSequence);
        firstIntentId.ShouldNotBe(secondIntentId);
        var pending = before.ShouldNotBeNull();
        pending.IntentId.ShouldBe(firstIntentId);
        pending.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        unrelated.ShouldNotBeNull().IntentId.ShouldBe(secondIntentId);
        beforeInbox.ShouldBeNull();

        var queued = originalWake.ShouldNotBeNull();
        queued.EventId.ShouldBe(eventId);
        queued.EventType.ShouldBe(CatalogGalleryPlacementQueuedIntegrationEvent.EventType);
        queued.PublishedAt.ShouldBeNull();
        queued.FailedAttempts.ShouldBe(0);
        var queuedPlacement = operationsBefore.ShouldNotBeNull();
        queuedPlacement.IntentId.ShouldBe(firstIntentId);
        queuedPlacement.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        queuedPlacement.OriginalWake.ShouldBe(new CatalogGalleryOriginalWakeDeliveryDto(queued.RecordedAt, null, 0, null, null));
        var waiting = failedWake.ShouldNotBeNull();
        waiting.StorageId.ShouldBe(queued.StorageId);
        waiting.FailedAttempts.ShouldBe(1);
        waiting.NextAttemptAt.ShouldBe(retryTime);
        waiting.PublishedAt.ShouldBeNull();
        var retryPlacement = operationsRetry.ShouldNotBeNull();
        retryPlacement.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        retryPlacement.OriginalWake.ShouldBe(new CatalogGalleryOriginalWakeDeliveryDto(
            waiting.RecordedAt, null, 1, waiting.LastAttemptAt, retryTime));
        var sent = published.ShouldNotBeNull();
        sent.StorageId.ShouldBe(queued.StorageId);
        sent.EventId.ShouldBe(queued.EventId);
        sent.FailedAttempts.ShouldBe(1);
        sent.NextAttemptAt.ShouldBeNull();
        sent.PublishedAt.ShouldNotBeNull().ShouldBeGreaterThanOrEqualTo(retryTime);
        var publishedPlacement = operationsPublished.ShouldNotBeNull();
        publishedPlacement.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        publishedPlacement.OriginalWake.ShouldBe(new CatalogGalleryOriginalWakeDeliveryDto(
            sent.RecordedAt, sent.PublishedAt, 1, sent.LastAttemptAt, null));
        publishedPlacement.TriggeringWake.ShouldBe(publishedPlacement.OriginalWake);
        var completed = inbox.ShouldNotBeNull();
        completed.Operation.ShouldBe(operation);
        completed.State.ShouldBe(IdempotencyEntryState.Completed);
        completed.CompletedAt.ShouldNotBeNull();
        var outcome = applied.ShouldNotBeNull();
        outcome.IntentId.ShouldBe(firstIntentId);
        outcome.AcceptedAt.ShouldBe(pending.AcceptedAt);
        outcome.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        var recorded = terminal.ShouldHaveSingleItem();
        recorded.Revision.Value.ShouldBe(outcome.Revision);
        var placement = recorded.Data.ShouldBeOfType<CatalogTourGalleryChanged>();
        placement.IntentId.ShouldBe(firstIntentId);
        placement.IntentSequence.ShouldBe(firstSequence);

        removed.ShouldBe(1);
        retainedAfterRemoval.ShouldBeNull();
        statusAfterRemoval.ShouldBe(outcome);
        var placementWithoutWake = publishedPlacement with
        {
            OriginalWake = null,
            TriggeringWake = null,
        };
        operationsAfterRemoval.ShouldBe(placementWithoutWake);
        var otherPlacement = otherTourOperations.ShouldNotBeNull();
        otherPlacement.IntentId.ShouldBe(secondIntentId);
        otherPlacement.OriginalWake.ShouldNotBeNull();
        missingPlacement.ShouldBeNull();
        firstByWake.ShouldBe(placementWithoutWake);
        var secondWakePlacement = secondByWake.ShouldNotBeNull();
        secondWakePlacement.CatalogTourId.ShouldBe(secondTour.Id);
        secondWakePlacement.Sequence.ShouldBe(firstSequence);
        secondWakePlacement.IntentId.ShouldBe(secondIntentId);
        missingWake.ShouldBeNull();
    }
}
