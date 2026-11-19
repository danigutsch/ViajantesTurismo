using PandaBear.Messaging;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Routes Catalog retirement completion to Admin and keeps internal Catalog delivery local.</summary>
/// <param name="localPublisher">The Catalog application dispatcher.</param>
/// <param name="adminTransport">The durable Admin queue publisher.</param>
internal sealed class CatalogOutboxEnvelopePublisher(
    IEventEnvelopePublisher localPublisher,
    IEventEnvelopePublisher adminTransport) : IEventEnvelopePublisher
{
    /// <inheritdoc />
    public async ValueTask Publish(EventEnvelope envelope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var publisher = string.Equals(envelope.EventType, CatalogTourRetirementAppliedIntegrationEvent.EventType, StringComparison.Ordinal)
            ? adminTransport
            : localPublisher;
        await publisher.Publish(envelope, ct).ConfigureAwait(false);
    }
}
