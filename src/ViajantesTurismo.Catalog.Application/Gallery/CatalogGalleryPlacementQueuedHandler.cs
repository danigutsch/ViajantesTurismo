using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.Application.Gallery;

/// <summary>Processes local gallery placement wake-ups independently of image processing.</summary>
internal sealed class CatalogGalleryPlacementQueuedHandler(
    CatalogTourPresentationService authoring) : IIntegrationEventHandler<CatalogGalleryPlacementQueuedIntegrationEvent>
{
    /// <inheritdoc />
    public ValueTask Handle(CatalogGalleryPlacementQueuedIntegrationEvent notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return authoring.ApplyGalleryIntents(notification.CatalogTourId, notification.Sequence, ct);
    }
}
