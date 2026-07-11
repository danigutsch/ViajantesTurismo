using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Application.Projections;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogProjectionRunnerTests
{
    [Fact]
    public async Task Project_replays_events_after_the_projection_checkpoint()
    {
        // Arrange
        var eventStore = new CapturingEventStore();
        var checkpointStore = new CapturingProjectionCheckpointStore
        {
            CurrentCheckpoint = new ProjectionCheckpoint("catalog.tours.read-model", 10),
        };
        var readModelStore = new CapturingCatalogTourReadModelStore();
        var projection = new CatalogTourReadModelProjection(readModelStore);
        var runner = new CatalogProjectionRunner(eventStore, checkpointStore, [projection]);
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7());
        var recordedAt = DateTimeOffset.UtcNow;
        eventStore.AddReplayEvent(CatalogProjectionRunnerTestsHelpers.CreateEnvelope(11, draftCreated, recordedAt));

        // Act
        var projectedEvents = await runner.Project(TestContext.Current.CancellationToken);

        // Assert
        projectedEvents.ShouldBe(1);
        (eventStore.LoadedAfterPosition).ShouldBe(10);
        (readModelStore.Draft).ShouldNotBeNull();
        (readModelStore.Draft.CatalogTourId).ShouldBe(draftCreated.CatalogTourId);
        (readModelStore.Draft.AdminTourId).ShouldBe(draftCreated.AdminTourId);
        (readModelStore.Draft.Identifier).ShouldBe(draftCreated.Identifier);
        (readModelStore.Draft.Title).ShouldBe(draftCreated.Title);
        (readModelStore.Draft.Position).ShouldBe(11);
        (readModelStore.Draft.UpdatedAt).ShouldBe(recordedAt);
        (checkpointStore.SavedCheckpoint).ShouldNotBeNull();
        (checkpointStore.SavedCheckpoint.ProjectionName).ShouldBe(projection.Name);
        (checkpointStore.SavedCheckpoint.Position).ShouldBe(11);
    }

    [Fact]
    public async Task Project_does_not_save_a_checkpoint_when_no_events_are_loaded()
    {
        // Arrange
        var eventStore = new CapturingEventStore();
        var checkpointStore = new CapturingProjectionCheckpointStore();
        var readModelStore = new CapturingCatalogTourReadModelStore();
        var projection = new CatalogTourReadModelProjection(readModelStore);
        var runner = new CatalogProjectionRunner(eventStore, checkpointStore, [projection]);

        // Act
        var projectedEvents = await runner.Project(TestContext.Current.CancellationToken);

        // Assert
        projectedEvents.ShouldBe(0);
        (eventStore.LoadedAfterPosition).ShouldBe(0);
        (readModelStore.Draft).ShouldBeNull();
        (checkpointStore.SavedCheckpoint).ShouldBeNull();
    }

    [Fact]
    public async Task Project_applies_events_in_position_order_and_checkpoints_the_highest_position()
    {
        // Arrange
        var eventStore = new CapturingEventStore();
        var checkpointStore = new CapturingProjectionCheckpointStore();
        var readModelStore = new CapturingCatalogTourReadModelStore();
        var projection = new CatalogTourReadModelProjection(readModelStore);
        var runner = new CatalogProjectionRunner(eventStore, checkpointStore, [projection]);
        var firstDraftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7());
        var secondDraftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "patagonia-2026",
            "Patagonia 2026",
            Guid.CreateVersion7());
        eventStore.AddReplayEvent(CatalogProjectionRunnerTestsHelpers.CreateEnvelope(12, secondDraftCreated, DateTimeOffset.UtcNow));
        eventStore.AddReplayEvent(CatalogProjectionRunnerTestsHelpers.CreateEnvelope(11, firstDraftCreated, DateTimeOffset.UtcNow));

        // Act
        var projectedEvents = await runner.Project(TestContext.Current.CancellationToken);

        // Assert
        projectedEvents.ShouldBe(2);
        (readModelStore.Drafts).ShouldMatchCollection(first => (first.Position).ShouldBe(11), second => (second.Position).ShouldBe(12));
        (checkpointStore.SavedCheckpoint).ShouldNotBeNull();
        (checkpointStore.SavedCheckpoint.Position).ShouldBe(12);
    }
}
