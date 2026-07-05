using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class CatalogEventEnvelopePublisher(IServiceProvider serviceProvider) : IEventEnvelopePublisher
{
    public async ValueTask Publish(EventEnvelope envelope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.Equals(envelope.EventType, AdminTourCreatedIntegrationEvent.EventType, StringComparison.Ordinal))
        {
            await Publish(
                Deserialize(envelope, CatalogIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent),
                ct).ConfigureAwait(false);
            return;
        }

        if (string.Equals(envelope.EventType, MediaImageOriginalStoredIntegrationEvent.EventType, StringComparison.Ordinal))
        {
            await Publish(
                Deserialize(envelope, CatalogIntegrationEventJsonContext.Default.MediaImageOriginalStoredIntegrationEvent),
                ct).ConfigureAwait(false);
            return;
        }

        throw new NotSupportedException($"Integration event type '{envelope.EventType}' is not configured for Catalog delivery.");
    }

    private async ValueTask Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        var handler = serviceProvider.GetRequiredService<IIntegrationEventHandler<TIntegrationEvent>>();
        await handler.Handle(integrationEvent, ct).ConfigureAwait(false);
    }

    private static TIntegrationEvent Deserialize<TIntegrationEvent>(
        EventEnvelope envelope,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<TIntegrationEvent> jsonTypeInfo)
        where TIntegrationEvent : IIntegrationEvent
    {
        if (envelope.Payload is null)
        {
            throw new InvalidOperationException($"Integration event '{envelope.EventType}' payload is required.");
        }

        return JsonSerializer.Deserialize(envelope.Payload, jsonTypeInfo)
            ?? throw new InvalidOperationException($"Integration event '{envelope.EventType}' payload could not be deserialized.");
    }
}
