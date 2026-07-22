using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CapturingEventStore(int appendFailures = 0) : IEventStore
{
    private readonly List<object> appendedEvents = [];
    private readonly List<EventEnvelope> appendedEnvelopes = [];
    private readonly List<EventEnvelope> replayEvents = [];
    private int remainingAppendFailures = appendFailures;

    public StreamId StreamId { get; private set; }

    public ExpectedStreamRevision ExpectedRevision { get; private set; }

    public IReadOnlyCollection<object> Events => appendedEvents;

    public long? LoadedAfterPosition { get; private set; }

    public int AppendAttempts { get; private set; }

    public void AddReplayEvent(EventEnvelope envelope) => replayEvents.Add(envelope);

    public ValueTask<IReadOnlyCollection<EventEnvelope>> Append(
        StreamId streamId,
        ExpectedStreamRevision expectedRevision,
        IReadOnlyCollection<object> events,
        CancellationToken ct)
    {
        AppendAttempts++;
        if (remainingAppendFailures > 0)
        {
            remainingAppendFailures--;
            throw new InvalidOperationException("append failed");
        }

        var currentStream = appendedEnvelopes
            .Concat(replayEvents)
            .Where(envelope => envelope.StreamId == streamId)
            .OrderBy(static envelope => envelope.Revision.Value)
            .ToArray();
        if (expectedRevision.RequiresEmptyStream && currentStream.Length > 0)
        {
            throw new ExpectedStreamRevisionConflictException(
                streamId,
                expectedRevision,
                currentStream[^1].Revision);
        }

        StreamId = streamId;
        ExpectedRevision = expectedRevision;
        appendedEvents.AddRange(events);
        var revision = expectedRevision.Value ?? 0;
        IReadOnlyCollection<EventEnvelope> envelopes = events
            .Select((domainEvent, index) => new EventEnvelope(
                streamId,
                index + 1,
                StreamRevision.From(revision + index + 1),
                Guid.CreateVersion7(),
                domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                domainEvent,
                DateTimeOffset.UtcNow))
            .ToArray();
        appendedEnvelopes.AddRange(envelopes);

        return ValueTask.FromResult(envelopes);
    }

    public ValueTask<IReadOnlyCollection<EventEnvelope>> Load(
        StreamId streamId,
        StreamRevision? afterRevision,
        CancellationToken ct)
    {
        var events = appendedEnvelopes
            .Concat(replayEvents)
            .Where(envelope => envelope.StreamId == streamId)
            .Where(envelope => afterRevision is null || envelope.Revision.Value > afterRevision.Value.Value)
            .OrderBy(static envelope => envelope.Revision.Value)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>(events);
    }

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
