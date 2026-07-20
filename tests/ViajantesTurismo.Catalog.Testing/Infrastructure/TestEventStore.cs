using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

internal sealed class TestEventStore : IEventStore
{
    private readonly Lock gate = new();
    private readonly Dictionary<StreamId, List<EventEnvelope>> streams = [];
    private long position;

    public ValueTask<IReadOnlyCollection<EventEnvelope>> Append(
        StreamId streamId,
        ExpectedStreamRevision expectedRevision,
        IReadOnlyCollection<object> events,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(events);
        ct.ThrowIfCancellationRequested();

        lock (gate)
        {
            return ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>(AppendCore(streamId, expectedRevision, events));
        }
    }

    public ValueTask<IReadOnlyCollection<EventEnvelope>> Load(
        StreamId streamId,
        StreamRevision? afterRevision,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (gate)
        {
            if (!streams.TryGetValue(streamId, out var stream))
            {
                return ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>([]);
            }

            var events = stream
                .Where(envelope => afterRevision is null || envelope.Revision.Value > afterRevision.Value.Value)
                .ToArray();
            return ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>(events);
        }
    }

    public ValueTask<IReadOnlyCollection<EventEnvelope>> LoadAfter(
        long position,
        int maxCount,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (gate)
        {
            var events = streams.Values
                .SelectMany(static stream => stream)
                .Where(envelope => envelope.Position > position)
                .OrderBy(envelope => envelope.Position)
                .Take(maxCount)
                .ToArray();
            return ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>(events);
        }
    }

    public void SeedTour(CatalogTourDraftReadModel tour)
    {
        ArgumentNullException.ThrowIfNull(tour);

        var streamId = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var initialSlug = CatalogTourSlug.IsCanonical(tour.Slug)
            ? tour.Slug
            : CatalogTourSlug.CreateInitial(tour.Identifier, tour.CatalogTourId);
        lock (gate)
        {
            _ = AppendCore(
                streamId,
                ExpectedStreamRevision.NoStream,
                [new CatalogTourDraftCreated(
                    tour.CatalogTourId,
                    tour.AdminTourId,
                    tour.Identifier,
                    tour.Title,
                    Guid.CreateVersion7(),
                    initialSlug)]);
            if (tour.IsPublished)
            {
                _ = AppendCore(
                    streamId,
                    ExpectedStreamRevision.From(StreamRevision.From(1)),
                    [new CatalogTourPresentationChanged(
                        tour.CatalogTourId,
                        tour.Title,
                        tour.Slug,
                        tour.Summary,
                        tour.Description,
                        tour.Itinerary,
                        tour.SeoTitle,
                        tour.SeoDescription)]);
                _ = AppendCore(
                    streamId,
                    ExpectedStreamRevision.From(StreamRevision.From(2)),
                    [new CatalogTourPublished(tour.CatalogTourId)]);
            }
        }
    }

    private List<EventEnvelope> AppendCore(
        StreamId streamId,
        ExpectedStreamRevision expectedRevision,
        IReadOnlyCollection<object> events)
    {
        var stream = GetOrCreateStream(streamId);
        EnsureExpectedRevision(streamId, stream.Count, expectedRevision);

        var appended = new List<EventEnvelope>(events.Count);
        foreach (var domainEvent in events)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            var revision = StreamRevision.From(stream.Count + 1L);
            var envelope = new EventEnvelope(
                streamId,
                ++position,
                revision,
                Guid.CreateVersion7(),
                domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                domainEvent,
                DateTimeOffset.UtcNow);
            stream.Add(envelope);
            appended.Add(envelope);
        }

        return appended;
    }

    private List<EventEnvelope> GetOrCreateStream(StreamId streamId)
    {
        if (!streams.TryGetValue(streamId, out var stream))
        {
            stream = [];
            streams.Add(streamId, stream);
        }

        return stream;
    }

    private static void EnsureExpectedRevision(
        StreamId streamId,
        int currentRevision,
        ExpectedStreamRevision expectedRevision)
    {
        if ((!expectedRevision.RequiresEmptyStream && expectedRevision.Value is null)
            || (expectedRevision.RequiresEmptyStream && currentRevision == 0)
            || expectedRevision.Value == currentRevision)
        {
            return;
        }

        throw new ExpectedStreamRevisionConflictException(
            streamId,
            expectedRevision,
            currentRevision == 0 ? null : StreamRevision.From(currentRevision));
    }
}
