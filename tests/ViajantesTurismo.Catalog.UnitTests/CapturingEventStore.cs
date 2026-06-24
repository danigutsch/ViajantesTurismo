using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CapturingEventStore : IEventStore
{
    private readonly List<object> appendedEvents = [];

    public StreamId StreamId { get; private set; }

    public ExpectedStreamRevision ExpectedRevision { get; private set; }

    public IReadOnlyCollection<object> Events => appendedEvents;

    public ValueTask Append(
        StreamId streamId,
        ExpectedStreamRevision expectedRevision,
        IReadOnlyCollection<object> events,
        CancellationToken ct)
    {
        StreamId = streamId;
        ExpectedRevision = expectedRevision;
        appendedEvents.AddRange(events);

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyCollection<EventEnvelope>> Load(
        StreamId streamId,
        StreamRevision? afterRevision,
        CancellationToken ct) => ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>([]);

    public ValueTask<IReadOnlyCollection<EventEnvelope>> LoadAfter(
        long position,
        int maxCount,
        CancellationToken ct) => ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>([]);
}
