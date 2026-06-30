using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CancelledEventStore : IEventStore
{
    public ValueTask Append(
        StreamId streamId,
        ExpectedStreamRevision expectedRevision,
        IReadOnlyCollection<object> events,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
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
