using SharedKernel.BuildingBlocks;
using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Coordinates event-sourced Catalog tour presentation and publication transitions.
/// </summary>
public sealed class CatalogTourPresentationService(
    IEventStore eventStore,
    ICatalogTourReadModelStore readModelStore,
    ICatalogTourSlugLock slugLock,
    CatalogTourReadModelProjection projection)
{
    /// <summary>
    /// Persists a customer-facing presentation edit and refreshes its read model.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="update">The validated presentation values.</param>
    /// <param name="expectedVersion">The stream version on which the edit is based.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The refreshed management projection, or <see langword="null" /> when the tour is missing.</returns>
    public async ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(
        Guid catalogTourId,
        CatalogTourPresentationUpdate update,
        long expectedVersion,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);

        var (aggregate, streamId) = await LoadTour(catalogTourId, expectedVersion, ct).ConfigureAwait(false);
        if (aggregate is null)
        {
            return null;
        }

        await using var slugLease = await slugLock.Acquire(update.Slug, ct).ConfigureAwait(false);
        if (!string.Equals(aggregate.Slug, update.Slug, StringComparison.Ordinal)
            && !await CatalogTourSlugAvailability.IsAvailable(
                eventStore,
                catalogTourId,
                update.Slug,
                ct).ConfigureAwait(false))
        {
            throw new CatalogTourSlugConflictException();
        }

        aggregate.ChangePresentation(
            update.Title,
            update.Slug,
            update.Summary,
            update.Description,
            update.Itinerary,
            update.SeoTitle,
            update.SeoDescription);
        return await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Explicitly publishes a Catalog tour and refreshes its read model.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="expectedVersion">The stream version on which the transition is based.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The refreshed management projection, or <see langword="null" /> when the tour is missing.</returns>
    public async ValueTask<CatalogTourDraftReadModel?> Publish(Guid catalogTourId, long expectedVersion, CancellationToken ct)
    {
        var (aggregate, streamId) = await LoadTour(catalogTourId, expectedVersion, ct).ConfigureAwait(false);
        if (aggregate is null)
        {
            return null;
        }

        aggregate.Publish();
        return await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Explicitly unpublishes a Catalog tour and refreshes its read model.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="expectedVersion">The stream version on which the transition is based.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The refreshed management projection, or <see langword="null" /> when the tour is missing.</returns>
    public async ValueTask<CatalogTourDraftReadModel?> Unpublish(Guid catalogTourId, long expectedVersion, CancellationToken ct)
    {
        var (aggregate, streamId) = await LoadTour(catalogTourId, expectedVersion, ct).ConfigureAwait(false);
        if (aggregate is null)
        {
            return null;
        }

        aggregate.Unpublish();
        return await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, ct).ConfigureAwait(false);
    }

    private async ValueTask<(CatalogTour? Aggregate, StreamId StreamId)> LoadTour(
        Guid catalogTourId,
        long expectedVersion,
        CancellationToken ct)
    {
        var tour = await readModelStore.GetTour(catalogTourId, ct).ConfigureAwait(false);
        if (tour is null)
        {
            return (null, default);
        }

        var streamId = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var events = await eventStore.Load(streamId, afterRevision: null, ct).ConfigureAwait(false);
        var aggregate = CatalogTour.Rehydrate(events.OrderBy(envelope => envelope.Revision.Value).Select(envelope => envelope.Data));
        if (aggregate.Id != catalogTourId)
        {
            throw new InvalidOperationException("Catalog tour read model and event stream identifiers do not match.");
        }

        EnsureExpectedVersion(streamId, aggregate.Version, expectedVersion);
        return (aggregate, streamId);
    }

    private static void EnsureExpectedVersion(StreamId streamId, long actualVersion, long expectedVersion)
    {
        if (actualVersion == expectedVersion)
        {
            return;
        }

        throw new ExpectedStreamRevisionConflictException(
            streamId,
            ExpectedStreamRevision.From(StreamRevision.From(expectedVersion)),
            StreamRevision.From(actualVersion));
    }

    private async ValueTask<CatalogTourDraftReadModel?> PersistAndProject(
        Guid catalogTourId,
        CatalogTour aggregate,
        StreamId streamId,
        long expectedVersion,
        CancellationToken ct)
    {
        var pendingEvents = aggregate.GetUncommittedEvents();
        if (pendingEvents.Count == 0)
        {
            return await readModelStore.GetTour(catalogTourId, ct).ConfigureAwait(false);
        }

        var persistedEvents = await eventStore.Append(
            streamId,
            ExpectedStreamRevision.From(StreamRevision.From(expectedVersion)),
            pendingEvents,
            ct).ConfigureAwait(false);
        aggregate.ClearUncommittedEvents();
        try
        {
            foreach (var envelope in persistedEvents.OrderBy(envelope => envelope.Revision.Value))
            {
                await projection.Apply(envelope, CancellationToken.None).ConfigureAwait(false);
            }

            return await readModelStore.GetTour(catalogTourId, CancellationToken.None).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The committed Catalog tour projection was not found.");
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(CancellationToken.None))
        {
            throw new CatalogTourProjectionPendingException(
                "The Catalog tour change was committed and is waiting for projection.",
                exception);
        }
    }
}
