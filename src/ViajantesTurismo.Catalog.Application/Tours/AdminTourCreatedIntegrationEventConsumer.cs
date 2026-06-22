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

        var catalogTour = CatalogTour.CreateDraft(
            notification.AdminTourId,
            notification.Identifier,
            notification.Name,
            notification.EventId);

        await eventStore.Append(
            CatalogTourStreamIds.FromAdminTourId(notification.AdminTourId),
            ExpectedStreamRevision.NoStream,
            catalogTour.GetUncommittedEvents(),
            ct);
        catalogTour.ClearUncommittedEvents();
    }
}
