using System.Diagnostics;
using SharedKernel.EventSourcing;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Creates a draft Catalog tour stream from an Admin tour-created integration event.
/// </summary>
public sealed class AdminTourCreatedIntegrationHandler(
    IEventStore eventStore,
    ICatalogTourSlugLock slugLock) : IIntegrationEventHandler<AdminTourCreatedIntegrationEvent>
{
    /// <inheritdoc />
    public async ValueTask Handle(AdminTourCreatedIntegrationEvent notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        using var activity = CatalogTelemetry.ActivitySource.StartActivity(
            CatalogTelemetry.ActivityTourStreamUpdate,
            ActivityKind.Internal);
        activity?.SetTag(CatalogTelemetry.TagBoundedContext, "catalog");
        activity?.SetTag(CatalogTelemetry.TagIntegrationEventType, AdminTourCreatedIntegrationEvent.EventType);
        activity?.SetTag(CatalogTelemetry.TagIntegrationEventVersion, AdminTourCreatedIntegrationEvent.EventVersion);

        try
        {
            ct.ThrowIfCancellationRequested();
            var preferredTour = CatalogTour.CreateDraft(
                notification.AdminTourId,
                notification.Identifier,
                notification.Name,
                notification.EventId);
            if (!await TryPersist(preferredTour, notification.EventId, ct).ConfigureAwait(false))
            {
                var fallbackTour = CatalogTour.CreateDraft(
                    preferredTour.Id,
                    notification.AdminTourId,
                    notification.Identifier,
                    notification.Name,
                    notification.EventId,
                    $"tour-{preferredTour.Id:N}");
                if (!await TryPersist(fallbackTour, notification.EventId, ct).ConfigureAwait(false))
                {
                    throw new CatalogTourSlugConflictException();
                }
            }

            activity?.SetTag(CatalogTelemetry.TagEventCount, 1);

            SetOutcome(activity, CatalogTelemetry.OutcomeSuccess);
            CatalogTelemetry.TourStreamUpdates.Add(1, CreateTags(CatalogTelemetry.OutcomeSuccess));
        }
        catch (OperationCanceledException ex)
        {
            if (!ex.ShouldHandleAsFailure(ct))
            {
                throw;
            }

            SetError(activity, ex);
            CatalogTelemetry.TourStreamUpdates.Add(1, CreateTags(CatalogTelemetry.OutcomeError));

            throw;
        }
        catch (Exception ex)
        {
            SetError(activity, ex);
            CatalogTelemetry.TourStreamUpdates.Add(1, CreateTags(CatalogTelemetry.OutcomeError));

            throw;
        }
    }

    private async ValueTask<bool> TryPersist(CatalogTour catalogTour, Guid sourceEventId, CancellationToken ct)
    {
        await using var slugLease = await slugLock.Acquire(catalogTour.Slug, ct).ConfigureAwait(false);
        if (!await CatalogTourSlugAvailability.IsAvailable(
            eventStore,
            catalogTour.Id,
            catalogTour.Slug,
            ct).ConfigureAwait(false))
        {
            return false;
        }

        var pendingEvents = catalogTour.GetUncommittedEvents();
        var streamId = CatalogTourStreamIds.FromAdminTourId(catalogTour.AdminTourId);
        try
        {
            await eventStore.Append(
                streamId,
                ExpectedStreamRevision.NoStream,
                pendingEvents,
                ct).ConfigureAwait(false);
        }
        catch (ExpectedStreamRevisionConflictException)
        {
            var existingEvents = await eventStore.Load(streamId, afterRevision: null, ct).ConfigureAwait(false);
            var initialEvent = existingEvents.FirstOrDefault(static envelope => envelope.Revision.Value == 1);
            if (initialEvent?.Data is not CatalogTourDraftCreated draftCreated
                || draftCreated.SourceEventId != sourceEventId)
            {
                throw;
            }
        }

        catalogTour.ClearUncommittedEvents();
        return true;
    }

    private static TagList CreateTags(string outcome)
    {
        return
        [
            new(CatalogTelemetry.TagIntegrationEventType, AdminTourCreatedIntegrationEvent.EventType),
            new(CatalogTelemetry.TagOutcome, outcome),
        ];
    }

    private static void SetOutcome(Activity? activity, string outcome)
    {
        activity?.SetTag(CatalogTelemetry.TagOutcome, outcome);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void SetError(Activity? activity, Exception exception)
    {
        activity?.SetTag(CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError);
        activity?.SetTag(CatalogTelemetry.TagErrorType, exception.GetType().Name);
        activity?.SetStatus(ActivityStatusCode.Error);
    }
}
