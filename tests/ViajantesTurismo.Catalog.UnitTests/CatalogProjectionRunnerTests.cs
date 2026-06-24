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
        eventStore.AddReplayEvent(CreateEnvelope(11, draftCreated, recordedAt));

        // Act
        await runner.Project(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(10, eventStore.LoadedAfterPosition);
        Assert.NotNull(readModelStore.Draft);
        Assert.Equal(draftCreated.CatalogTourId, readModelStore.Draft.CatalogTourId);
        Assert.Equal(draftCreated.AdminTourId, readModelStore.Draft.AdminTourId);
        Assert.Equal(draftCreated.Identifier, readModelStore.Draft.Identifier);
        Assert.Equal(draftCreated.Title, readModelStore.Draft.Title);
        Assert.Equal(11, readModelStore.Draft.Position);
        Assert.Equal(recordedAt, readModelStore.Draft.UpdatedAt);
        Assert.NotNull(checkpointStore.SavedCheckpoint);
        Assert.Equal(projection.Name, checkpointStore.SavedCheckpoint.ProjectionName);
        Assert.Equal(11, checkpointStore.SavedCheckpoint.Position);
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
        await runner.Project(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, eventStore.LoadedAfterPosition);
        Assert.Null(readModelStore.Draft);
        Assert.Null(checkpointStore.SavedCheckpoint);
    }

    private static EventEnvelope CreateEnvelope(long position, CatalogTourDraftCreated draftCreated, DateTimeOffset recordedAt)
    {
        return new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(draftCreated.AdminTourId),
            position,
            StreamRevision.From(1),
            Guid.CreateVersion7(),
            nameof(CatalogTourDraftCreated),
            draftCreated,
            recordedAt);
    }
}
