using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.Application.Projections;

/// <summary>
/// Replays event-store events into Catalog projections and persists projection checkpoints.
/// </summary>
public sealed class CatalogProjectionRunner(
    IEventStore eventStore,
    IProjectionCheckpointStore checkpointStore,
    IEnumerable<IProjection> projections)
{
    private const int BatchSize = 100;

    /// <summary>
    /// Projects one batch of events for each configured projection.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    public async ValueTask Project(CancellationToken ct)
    {
        foreach (var projection in projections)
        {
            var checkpoint = await checkpointStore.GetCheckpoint(projection.Name, ct);
            var position = checkpoint?.Position ?? 0;
            var envelopes = await eventStore.LoadAfter(position, BatchSize, ct);
            if (envelopes.Count == 0)
            {
                continue;
            }

            var lastPosition = position;
            foreach (var envelope in envelopes.OrderBy(static envelope => envelope.Position))
            {
                await projection.Apply(envelope, ct);
                lastPosition = envelope.Position;
            }

            await checkpointStore.Save(new ProjectionCheckpoint(projection.Name, lastPosition), ct);
        }
    }
}
