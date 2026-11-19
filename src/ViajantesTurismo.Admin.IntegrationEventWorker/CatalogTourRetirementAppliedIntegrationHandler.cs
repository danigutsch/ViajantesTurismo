using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Admin.IntegrationEventWorker;

/// <summary>Records the first Catalog acknowledgement for the matching Admin retirement.</summary>
internal sealed class CatalogTourRetirementAppliedIntegrationHandler(ITourRetirementCompletion completion)
    : IIntegrationEventHandler<CatalogTourRetirementAppliedIntegrationEvent>
{
    /// <inheritdoc />
    public async ValueTask Handle(CatalogTourRetirementAppliedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await completion.RecordApplied(
            integrationEvent.AdminTourId,
            integrationEvent.RetirementId,
            integrationEvent.OccurredAt,
            ct).ConfigureAwait(false);
    }
}
