using System.Diagnostics;
using SharedKernel.EventSourcing;
using SharedKernel.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Creates a draft Catalog tour stream from an Admin tour-created integration event.
/// </summary>
public sealed class AdminTourCreatedIntegrationEventConsumer(
    IEventStore eventStore) : IIntegrationEventHandler<AdminTourCreatedIntegrationEvent>
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

        var catalogTour = CatalogTour.CreateDraft(
            notification.AdminTourId,
            notification.Identifier,
            notification.Name,
            notification.EventId);

        var pendingEvents = catalogTour.GetUncommittedEvents();
        activity?.SetTag(CatalogTelemetry.TagEventCount, pendingEvents.Count);
        try
        {
            await eventStore.Append(
                CatalogTourStreamIds.FromAdminTourId(notification.AdminTourId),
                ExpectedStreamRevision.NoStream,
                pendingEvents,
                ct);
            catalogTour.ClearUncommittedEvents();

            SetOutcome(activity, CatalogTelemetry.OutcomeSuccess);
            CatalogTelemetry.TourStreamUpdates.Add(1, CreateTags(CatalogTelemetry.OutcomeSuccess));
        }
        catch (Exception ex)
        {
            SetError(activity, ex);
            CatalogTelemetry.TourStreamUpdates.Add(1, CreateTags(CatalogTelemetry.OutcomeError));

            throw;
        }
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
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddException(exception);
    }
}
