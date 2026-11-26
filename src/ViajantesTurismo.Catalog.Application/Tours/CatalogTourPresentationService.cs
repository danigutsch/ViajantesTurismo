using SharedKernel.BuildingBlocks;
using SharedKernel.EventSourcing;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Coordinates event-sourced Catalog tour presentation, publication and gallery changes.
/// </summary>
public sealed class CatalogTourPresentationService(
    IEventStore eventStore,
    ICatalogTourReadModelStore readModelStore,
    ICatalogTourSlugLock slugLock,
    ICatalogTourMutationLock mutationLock,
    CatalogTourReadModelProjection projection,
    IPublicMediaImageStore imageStore,
    ICatalogGalleryPlacementIntentStore placementIntents)
{
    /// <summary>
    /// Persists a customer-facing presentation edit and refreshes its read model.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="update">The validated presentation values.</param>
    /// <param name="expectedVersion">The stream version on which the edit is based.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The refreshed management projection, a not-found result when the tour is missing, or the domain failure.
    /// </returns>
    public async ValueTask<Result<CatalogTourDraftReadModel>> UpdatePresentation(
        Guid catalogTourId,
        CatalogTourPresentationUpdate update,
        long expectedVersion,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);

        var (aggregate, streamId, _, _) = await LoadTour(catalogTourId, expectedVersion, ct).ConfigureAwait(false);
        if (aggregate is null)
        {
            return TourNotFound();
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

        var changed = aggregate.ChangePresentation(
            update.Title,
            update.Slug,
            update.Summary,
            update.Description,
            update.Itinerary,
            update.SeoTitle,
            update.SeoDescription);
        if (changed.IsFailure)
        {
            return changed.ConvertError<CatalogTourDraftReadModel>();
        }

        return await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, causationEventId: null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Explicitly publishes a Catalog tour and refreshes its read model.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="expectedVersion">The stream version on which the transition is based.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The refreshed management projection, a not-found result when the tour is missing, or the domain failure.
    /// </returns>
    public async ValueTask<Result<CatalogTourDraftReadModel>> Publish(Guid catalogTourId, long expectedVersion, CancellationToken ct)
    {
        await using var mutationLease = await mutationLock.Acquire([catalogTourId], ct).ConfigureAwait(false);
        var (aggregate, streamId, _, _) = await LoadTour(catalogTourId, expectedVersion, ct).ConfigureAwait(false);
        if (aggregate is null)
        {
            return TourNotFound();
        }

        var published = aggregate.Publish();
        if (published.IsFailure)
        {
            return published.ConvertError<CatalogTourDraftReadModel>();
        }

        return await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, causationEventId: null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Explicitly unpublishes a Catalog tour and refreshes its read model.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="expectedVersion">The stream version on which the transition is based.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The refreshed management projection, or a not-found result when the tour is missing.</returns>
    public async ValueTask<Result<CatalogTourDraftReadModel>> Unpublish(Guid catalogTourId, long expectedVersion, CancellationToken ct)
    {
        await using var mutationLease = await mutationLock.Acquire([catalogTourId], ct).ConfigureAwait(false);
        var (aggregate, streamId, _, _) = await LoadTour(catalogTourId, expectedVersion, ct).ConfigureAwait(false);
        if (aggregate is null)
        {
            return TourNotFound();
        }

        aggregate.Unpublish();
        return await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, causationEventId: null, ct).ConfigureAwait(false);
    }

    /// <summary>Commits the authored ordering of photos linked to the tour and refreshes its projection.</summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="photoIds">The photos in authored display order.</param>
    /// <param name="coverId">The editor's optional cover selection.</param>
    /// <param name="expectedVersion">The stream revision on which the edit is based.</param>
    /// <param name="canPublish">Whether the caller has Catalog publish permission.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The refreshed tour projection or the reason the placement could not be changed.</returns>
    public async ValueTask<Result<CatalogTourDraftReadModel>> ChangeGallery(
        Guid catalogTourId, IReadOnlyList<Guid> photoIds, Guid? coverId, long expectedVersion, bool canPublish, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(photoIds);

        await using var mutationLease = await mutationLock.Acquire([catalogTourId], ct).ConfigureAwait(false);
        var (aggregate, streamId, current, _) = await LoadTour(catalogTourId, expectedVersion, ct).ConfigureAwait(false);
        if (aggregate is null || current is null)
        {
            return TourNotFound();
        }

        if (!aggregate.CanEditGallery(canPublish, current.IsPubliclyVisible, current.StreamVersion))
        {
            return Result.Forbidden<CatalogTourDraftReadModel>("The current tour does not permit gallery changes by this caller.");
        }

        if (aggregate.GalleryIntentSequence != await placementIntents.GetHighWatermark(catalogTourId, ct).ConfigureAwait(false))
        {
            return Result.Conflict<CatalogTourDraftReadModel>("Accepted gallery placement is still pending.");
        }

        var images = await imageStore.ListByTour(catalogTourId, ct).ConfigureAwait(false);
        var changed = aggregate.ChangeGallery(photoIds, coverId, new CatalogGalleryPhotoInventory(images));
        if (changed.IsFailure)
        {
            return changed.ConvertError<CatalogTourDraftReadModel>();
        }

        if (aggregate.GetUncommittedEvents().Count == 0)
        {
            if (current.StreamVersion != expectedVersion || current.GalleryPhotoIds is null
                || !current.GalleryPhotoIds.SequenceEqual(photoIds) || current.GalleryCoverId != coverId)
            {
                throw new CatalogTourProjectionPendingException();
            }

            return Result.Ok(current);
        }

        return await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, causationEventId: null, ct).ConfigureAwait(false);
    }

    /// <summary>Queues an authorized gallery snapshot once the editor's observed placement has finished applying.</summary>
    /// <param name="catalogTourId">The affected tour.</param>
    /// <param name="photoIds">The complete desired photo order.</param>
    /// <param name="coverId">The editor's optional cover selection.</param>
    /// <param name="expectedVersion">The observed Catalog stream revision.</param>
    /// <param name="expectedPlacementSequence">The latest accepted placement sequence observed by the editor.</param>
    /// <param name="canPublish">Whether the caller holds Catalog publication authority.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The new sequence, or a rejection if the editor needs a refreshed gallery.</returns>
    public async ValueTask<Result<long>> QueueGallery(
        Guid catalogTourId, IReadOnlyList<Guid> photoIds, Guid? coverId, long expectedVersion, long expectedPlacementSequence, bool canPublish,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(photoIds);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedPlacementSequence);

        await using var mutationLease = await mutationLock.Acquire([catalogTourId], ct).ConfigureAwait(false);
        var (aggregate, _, current, _) = await LoadTour(catalogTourId, expectedVersion, ct).ConfigureAwait(false);
        if (aggregate is null || current is null)
        {
            return Result.NotFound<long>("The Catalog tour was not found.");
        }

        if (!aggregate.CanEditGallery(canPublish, current.IsPubliclyVisible, current.StreamVersion))
        {
            return Result.Forbidden<long>("The current tour does not permit gallery changes by this caller.");
        }

        var acceptedSequence = await placementIntents.GetHighWatermark(catalogTourId, ct).ConfigureAwait(false);
        if (expectedPlacementSequence != acceptedSequence)
        {
            return Result.Conflict<long>("Gallery placement changed. Read the current gallery before editing.");
        }

        if (aggregate.GalleryIntentSequence != acceptedSequence || current.StreamVersion != expectedVersion)
        {
            return Result.Conflict<long>("Accepted gallery placement is still pending.");
        }

        var images = await imageStore.ListByTour(catalogTourId, ct).ConfigureAwait(false);
        var validated = aggregate.ChangeGallery(photoIds, coverId, new CatalogGalleryPhotoInventory(images));
        if (validated.IsFailure)
        {
            return validated.ConvertError<long>();
        }

        var sequence = await placementIntents.Enqueue(catalogTourId, photoIds, coverId, canPublish, ct).ConfigureAwait(false);
        return Result.Ok(sequence);
    }

    /// <summary>Reads current editor tokens and the requested placement outcome without mutating Catalog.</summary>
    /// <param name="catalogTourId">The affected tour.</param>
    /// <param name="placementSequence">The outcome sequence, or null for the latest accepted placement.</param>
    /// <param name="canPublish">Whether the caller can edit published Catalog galleries.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The current editor state or a not-found result for the tour or placement.</returns>
    public async ValueTask<Result<CatalogGalleryEditorStateDto>> GetGalleryEditorState(
        Guid catalogTourId, long? placementSequence, bool canPublish, CancellationToken ct)
    {
        if (placementSequence is { } requested)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requested, nameof(placementSequence));
        }

        await using var mutationLease = await mutationLock.Acquire([catalogTourId], ct).ConfigureAwait(false);
        var (aggregate, _, current, events) = await LoadTour(catalogTourId, expectedVersion: null, ct).ConfigureAwait(false);
        if (aggregate is null || current is null)
        {
            return Result.NotFound<CatalogGalleryEditorStateDto>("The Catalog tour was not found.");
        }

        var acceptedSequence = await placementIntents.GetHighWatermark(catalogTourId, ct).ConfigureAwait(false);
        var sequence = placementSequence ?? acceptedSequence;
        var isRetired = aggregate.RetirementId is not null;
        CatalogGalleryPlacementOutcomeDto? outcome = null;
        if (sequence > 0)
        {
            var intent = await placementIntents.GetIntent(catalogTourId, sequence, ct).ConfigureAwait(false);
            if (intent is null)
            {
                return Result.NotFound<CatalogGalleryEditorStateDto>("The Catalog placement was not found.");
            }

            outcome = new CatalogGalleryPlacementOutcomeDto(intent.Id, sequence, GetPlacementStatus(intent, events, isRetired));
        }

        var canEdit = aggregate.CanEditGallery(canPublish, current.IsPubliclyVisible, current.StreamVersion);
        return Result.Ok(new CatalogGalleryEditorStateDto(aggregate.Version, current.StreamVersion, acceptedSequence,
            aggregate.GalleryIntentSequence, aggregate.GalleryPhotoIds, aggregate.GalleryCoverId, isRetired, canEdit,
            CatalogDomainLimits.MaxGalleryPhotos, outcome));
    }

    /// <summary>Seeds the authored snapshot from current Catalog links, with resumption managed by the tour projection.</summary>
    /// <param name="catalogTourId">The tour whose legacy album should be initialized.</param>
    /// <param name="canPublish">Whether the caller can edit published Catalog galleries.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The initialized tour projection or the reason initialization is unavailable.</returns>
    public async ValueTask<Result<CatalogTourDraftReadModel>> BootstrapLegacyGallery(Guid catalogTourId, bool canPublish, CancellationToken ct)
    {
        await using var mutationLease = await mutationLock.Acquire([catalogTourId], ct).ConfigureAwait(false);
        var (aggregate, streamId, current, _) = await LoadTour(catalogTourId, expectedVersion: null, ct).ConfigureAwait(false);
        if (aggregate is null || current is null)
        {
            return TourNotFound();
        }

        if (aggregate.GalleryPhotoIds is not null)
        {
            return current.GalleryPhotoIds is null
                ? throw new CatalogTourProjectionPendingException()
                : Result.Ok(current);
        }

        if (!aggregate.CanEditGallery(canPublish, current.IsPubliclyVisible, current.StreamVersion))
        {
            return Result.Forbidden<CatalogTourDraftReadModel>("The current tour does not permit gallery changes by this caller.");
        }

        var images = await imageStore.ListByTour(catalogTourId, ct).ConfigureAwait(false);
        var legacy = images.OrderBy(image => image.GetDisplayOrderForTour(catalogTourId)).ThenBy(image => image.Id).ToArray();
        var photoIds = legacy.Select(image => image.Id).ToArray();
        var coverId = legacy.FirstOrDefault(image => image.IsCoverForTour(catalogTourId))?.Id;
        var throughSequence = await placementIntents.GetHighWatermark(catalogTourId, ct).ConfigureAwait(false);
        var expectedVersion = aggregate.Version;
        var bootstrapped = aggregate.BootstrapLegacyGallery(photoIds, coverId, new CatalogGalleryPhotoInventory(images), throughSequence);
        if (bootstrapped.IsFailure)
        {
            return bootstrapped.ConvertError<CatalogTourDraftReadModel>();
        }

        return await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, causationEventId: null, ct).ConfigureAwait(false);
    }

    /// <summary>Drains committed placement intents in tour order up to the supplied wake-up sequence.</summary>
    public ValueTask ApplyGalleryIntents(Guid catalogTourId, long throughSequence, CancellationToken ct) =>
        ApplyGalleryIntents(catalogTourId, throughSequence, causationEventId: null, ct);

    /// <summary>Drains committed intents and records the wake responsible for the resulting appends when supplied.</summary>
    internal async ValueTask ApplyGalleryIntents(Guid catalogTourId, long throughSequence, Guid? causationEventId, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(throughSequence);

        await using var mutationLease = await mutationLock.Acquire([catalogTourId], ct).ConfigureAwait(false);
        var (aggregate, streamId, current, _) = await LoadTour(catalogTourId, expectedVersion: null, ct).ConfigureAwait(false);
        if (aggregate is null || current is null)
        {
            throw new InvalidOperationException("The Catalog tour for the placement intent is missing.");
        }

        if (aggregate.RetirementId is not null)
        {
            // The durable retirement event closes all placement work for this tour.
            return;
        }

        while (aggregate.GalleryIntentSequence < throughSequence)
        {
            var next = aggregate.GalleryIntentSequence + 1;
            var intent = await placementIntents.GetIntent(catalogTourId, next, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("An earlier gallery placement intent is still pending.");
            Guid? uploadedPhotoId = intent.AppendsUploadedPhoto ? intent.PhotoIds.Single() : null;
            if (uploadedPhotoId.HasValue && aggregate.GalleryPhotoIds is null)
            {
                // Legacy uploads remain journaled for the bootstrap to wake once it supplies the authored baseline.
                return;
            }

            var isAlreadyPlaced = uploadedPhotoId is { } photoId && aggregate.IncludesBootstrappedUpload(photoId, intent.Sequence);
            var expectedVersion = aggregate.Version;
            var rejected = !isAlreadyPlaced && aggregate.RejectGalleryIntentAfterPublication(intent.CanPublish, intent.Id, intent.Sequence);
            if (!rejected)
            {
                if (!isAlreadyPlaced && !aggregate.CanEditGallery(intent.CanPublish, current.IsPubliclyVisible, current.StreamVersion))
                {
                    throw new CatalogTourProjectionPendingException();
                }

                var images = await imageStore.ListByTour(catalogTourId, ct).ConfigureAwait(false);
                var inventory = new CatalogGalleryPhotoInventory(images);
                var changed = uploadedPhotoId is { } upload
                    ? aggregate.ApplyGalleryUpload(upload, inventory, intent.Id, intent.Sequence)
                    : aggregate.ApplyGalleryIntent(intent.PhotoIds, intent.CoverId, inventory, intent.Id, intent.Sequence);
                if (changed.IsFailure)
                {
                    throw new InvalidOperationException("The queued placement no longer passes Catalog gallery validation.");
                }
            }

            var result = await PersistAndProject(catalogTourId, aggregate, streamId, expectedVersion, causationEventId, ct).ConfigureAwait(false);
            current = result.Value;
        }
    }

    private async ValueTask<(CatalogTour? Aggregate, StreamId StreamId, CatalogTourDraftReadModel? Projection, IReadOnlyCollection<EventEnvelope> Events)> LoadTour(
        Guid catalogTourId,
        long? expectedVersion,
        CancellationToken ct)
    {
        var tour = await readModelStore.GetTour(catalogTourId, ct).ConfigureAwait(false);
        if (tour is null)
        {
            return (null, default, null, []);
        }

        var streamId = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var events = await eventStore.Load(streamId, afterRevision: null, ct).ConfigureAwait(false);
        var aggregate = CatalogTour.Rehydrate(events.OrderBy(envelope => envelope.Revision.Value).Select(envelope => envelope.Data));
        if (aggregate.Id != catalogTourId)
        {
            throw new InvalidOperationException("Catalog tour read model and event stream identifiers do not match.");
        }

        if (expectedVersion is { } expected)
        {
            EnsureExpectedVersion(streamId, aggregate.Version, expected);
        }

        return (aggregate, streamId, tour, events);
    }

    private static CatalogGalleryPlacementStatusDto GetPlacementStatus(
        CatalogGalleryPlacementIntent intent, IReadOnlyCollection<EventEnvelope> events, bool isRetired)
    {
        foreach (var envelope in events)
        {
            switch (envelope.Data)
            {
                case CatalogTourGalleryChanged applied when applied.IntentId == intent.Id && applied.IntentSequence == intent.Sequence:
                    return CatalogGalleryPlacementStatusDto.Applied;
                case CatalogTourGalleryIntentRejected rejected when rejected.IntentId == intent.Id && rejected.IntentSequence == intent.Sequence:
                    return CatalogGalleryPlacementStatusDto.RejectedAfterPublication;
            }
        }

        return isRetired ? CatalogGalleryPlacementStatusDto.ClosedByRetirement : CatalogGalleryPlacementStatusDto.Pending;
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

    private static Result<CatalogTourDraftReadModel> TourNotFound() =>
        Result.NotFound<CatalogTourDraftReadModel>("The Catalog tour was not found.");

    private async ValueTask<Result<CatalogTourDraftReadModel>> PersistAndProject(
        Guid catalogTourId,
        CatalogTour aggregate,
        StreamId streamId,
        long expectedVersion,
        Guid? causationEventId,
        CancellationToken ct)
    {
        var pendingEvents = aggregate.GetUncommittedEvents();
        if (pendingEvents.Count == 0)
        {
            var current = await readModelStore.GetTour(catalogTourId, ct).ConfigureAwait(false);
            return current is null ? TourNotFound() : Result.Ok(current);
        }

        var expectedRevision = ExpectedStreamRevision.From(StreamRevision.From(expectedVersion));
        var persistedEvents = causationEventId is { } cause
            ? await eventStore.Append(streamId, expectedRevision, pendingEvents, cause, ct).ConfigureAwait(false)
            : await eventStore.Append(streamId, expectedRevision, pendingEvents, ct).ConfigureAwait(false);
        aggregate.ClearUncommittedEvents();
        try
        {
            foreach (var envelope in persistedEvents.OrderBy(envelope => envelope.Revision.Value))
            {
                await projection.Apply(envelope, CancellationToken.None).ConfigureAwait(false);
            }

            return Result.Ok(await readModelStore.GetTour(catalogTourId, CancellationToken.None).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The committed Catalog tour projection was not found."));
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(CancellationToken.None))
        {
            throw new CatalogTourProjectionPendingException(
                "The Catalog tour change was committed and is waiting for projection.",
                exception);
        }
    }
}
