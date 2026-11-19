using PandaBear.EventSourcing;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>Closes the Catalog stream and durably withdraws the retired tour from public reads.</summary>
public sealed class AdminTourRetiredIntegrationHandler(
    IEventStore eventStore,
    ICatalogTourRetirementStore retirements) : IIntegrationEventHandler<AdminTourRetiredIntegrationEvent>
{
    /// <inheritdoc />
    public async ValueTask Handle(AdminTourRetiredIntegrationEvent notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var streamId = CatalogTourStreamIds.FromAdminTourId(notification.AdminTourId);
        for (var attempt = 1; ; attempt++)
        {
            var stream = await eventStore.Load(streamId, afterRevision: null, ct).ConfigureAwait(false);
            var tour = CatalogTour.Rehydrate(stream.OrderBy(envelope => envelope.Revision.Value).Select(envelope => envelope.Data));
            var expected = tour.Version == 0
                ? ExpectedStreamRevision.NoStream
                : ExpectedStreamRevision.From(StreamRevision.From(tour.Version));
            tour.Retire(notification.AdminTourId, notification.RetirementId);
            var pending = tour.GetUncommittedEvents();
            if (pending.Count > 0)
            {
                try
                {
                    await eventStore.Append(streamId, expected, pending, ct).ConfigureAwait(false);
                }
                catch (ExpectedStreamRevisionConflictException) when (attempt < 4)
                {
                    continue;
                }
            }

            await retirements.Withdraw(new CatalogTourRetired(notification.AdminTourId, notification.RetirementId), ct).ConfigureAwait(false);
            return;
        }
    }
}
