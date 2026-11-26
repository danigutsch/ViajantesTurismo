using Npgsql;
using SharedKernel.EventSourcing;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies that a new active Catalog host resumes the same persisted Gallery delivery.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryRelayRestartPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task A_restarted_host_retries_the_same_pending_wake_and_commits_one_outcome()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        Guid tourId;
        Guid intentId;
        Guid photoId;
        StreamId stream;
        long sequence;
        await using (var writer = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct))
        {
            var tour = await writer.SeedDraft(authoredGallery: false, ct);
            tourId = tour.Id;
            stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
            var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
            photoId = photo.Id;
            await writer.Images.Upsert(photo, ct);
            var accepted = await writer.Writer.QueueGallery(tourId, [photoId], photoId, 1, 0, canPublish: false, ct);
            sequence = accepted.Value;
            var stored = await writer.Intents.GetIntent(tourId, sequence, ct);
            intentId = stored.ShouldNotBeNull().Id;
            await writer.Events.Append(stream, ExpectedStreamRevision.From(StreamRevision.From(1)),
                [new CatalogTourPresentationChanged(tourId, "Andes north", tour.Slug, "Ride the Andes.", "", "", "", "")], ct);
        }

        // Act
        CatalogGalleryWakeDelivery failed;
        TimeSpan inboxLease;
        var firstTime = TimeProvider.System.GetUtcNow();
        await using (var firstHost = await CatalogGalleryRelayHost.Start(database.ConnectionString, firstTime, ct))
        {
            failed = await CatalogGalleryWakeDelivery.WaitFor(dataSource, intentId, published: false, ct);
            inboxLease = firstHost.InboxLeaseDuration;
        }

        CatalogGalleryEditorStateDto pending;
        await using (var projector = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct))
        {
            var state = await projector.Writer.GetGalleryEditorState(tourId, sequence, canPublish: false, ct);
            pending = state.Value;
            await projector.Runner.Project(ct);
        }

        var retryAt = failed.NextAttemptAt.ShouldNotBeNull();
        CatalogGalleryWakeDelivery published;
        await using (var restartedHost = await CatalogGalleryRelayHost.Start(database.ConnectionString, retryAt + inboxLease, ct))
        {
            published = await CatalogGalleryWakeDelivery.WaitFor(dataSource, intentId, published: true, ct);
        }

        await using var observer = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var final = await observer.Writer.GetGalleryEditorState(tourId, sequence, canPublish: false, ct);
        var newEvents = await observer.Events.Load(stream, afterRevision: StreamRevision.From(2), ct);

        // Assert
        var before = pending.Outcome.ShouldNotBeNull();
        before.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        before.IntentId.ShouldBe(intentId);
        pending.ProjectedStreamVersion.ShouldBeLessThan(pending.StreamVersion);
        failed.EventId.ShouldBe(intentId.ToString("D"));
        failed.FailedAttempts.ShouldBe(1);
        failed.PublishedAt.ShouldBeNull();
        failed.LastAttemptAt.ShouldBeLessThan(retryAt);
        published.StorageId.ShouldBe(failed.StorageId);
        published.EventId.ShouldBe(failed.EventId);
        published.FailedAttempts.ShouldBe(failed.FailedAttempts);
        published.PublishedAt.ShouldNotBeNull().ShouldBeGreaterThanOrEqualTo(retryAt);
        published.NextAttemptAt.ShouldBeNull();
        var outcome = final.Value.Outcome.ShouldNotBeNull();
        outcome.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        outcome.IntentId.ShouldBe(before.IntentId);
        outcome.Sequence.ShouldBe(sequence);
        final.Value.ResolvedPlacementSequence.ShouldBe(sequence);
        final.Value.PhotoIds.ShouldNotBeNull().ShouldBe([photoId]);
        var committed = newEvents.ShouldHaveSingleItem();
        committed.Revision.Value.ShouldBe(final.Value.StreamVersion);
        committed.RecordedAt.ShouldBeLessThanOrEqualTo(published.PublishedAt.ShouldNotBeNull());
        var applied = committed.Data.ShouldBeOfType<CatalogTourGalleryChanged>();
        applied.IntentId.ShouldBe(intentId);
        applied.IntentSequence.ShouldBe(sequence);
    }
}
