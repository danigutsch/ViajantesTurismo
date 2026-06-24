using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CapturingEventStore : IEventStore
{
    private readonly List<object> appendedEvents = [];
    private readonly List<EventEnvelope> replayEvents = [];

    public StreamId StreamId { get; private set; }

    public ExpectedStreamRevision ExpectedRevision { get; private set; }

    public IReadOnlyCollection<object> Events => appendedEvents;

    public long? LoadedAfterPosition { get; private set; }

    public void AddReplayEvent(EventEnvelope envelope) => replayEvents.Add(envelope);

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
        CancellationToken ct)
    {
        LoadedAfterPosition = position;
        var events = replayEvents
            .Where(envelope => envelope.Position > position)
            .Take(maxCount)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>(events);
    }
}
