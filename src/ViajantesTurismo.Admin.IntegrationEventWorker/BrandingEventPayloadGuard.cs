using PandaBear.Messaging;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.IntegrationEventWorker;

/// <summary>Checks the Branding payload before the generated dispatcher can decode it.</summary>
internal sealed class BrandingEventPayloadGuard(
    [FromKeyedServices(AdminWorkerIntegrationEvents.GeneratedPublisherKey)] IEventEnvelopePublisher publisher) : IEventEnvelopePublisher
{
    /// <inheritdoc />
    public ValueTask Publish(EventEnvelope envelope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ct.ThrowIfCancellationRequested();

        if (string.Equals(envelope.EventType, BrandingSettingsChangedIntegrationEvent.EventType, StringComparison.Ordinal)
            && envelope.Payload is { } payload)
        {
            BrandingEventLimits.EnsurePayloadFits(payload);
        }

        return publisher.Publish(envelope, ct);
    }
}
