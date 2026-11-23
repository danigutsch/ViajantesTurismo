using System.Text.Json.Serialization.Metadata;

using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.RepositoryDocumentation.AsyncApi;

/// <summary>Describes a durable event and its application topology.</summary>
/// <param name="Contract">The production JSON serializer metadata.</param>
/// <param name="EventType">The durable event type and channel address.</param>
/// <param name="EventVersion">The published payload contract version.</param>
/// <param name="Title">The display name for the event.</param>
/// <param name="Producer">The bounded context that publishes the event.</param>
/// <param name="Consumer">The bounded context that handles the event.</param>
/// <param name="ConsumerName">The logical name of the consumer.</param>
/// <param name="Outbox">The producer database context that stores the outbox record.</param>
/// <param name="Inbox">The consumer database context that stores the idempotency record.</param>
/// <param name="Handler">The qualified name of the consuming handler.</param>
/// <param name="TransportConsumer">The database context that claims messages from a remote producer.</param>
/// <param name="DomainEventMapping">The qualified mapping method name if the event originated in a domain event.</param>
internal sealed record AsyncApiEvent(
    JsonTypeInfo Contract,
    string EventType,
    int EventVersion,
    string Title,
    string Producer,
    string Consumer,
    string ConsumerName,
    string Outbox,
    string Inbox,
    string Handler,
    string? TransportConsumer,
    string? DomainEventMapping)
{
    /// <summary>Reads the type and version from a durable event backed by its production serializer.</summary>
    internal static AsyncApiEvent Describe<TEvent>(
        JsonTypeInfo<TEvent> contract,
        string title,
        string producer,
        string consumer,
        string consumerName,
        string outbox,
        string inbox,
        string handler,
        string? transportConsumer = null,
        string? domainEventMapping = null)
        where TEvent : IIntegrationEvent
        => new(contract, TEvent.EventType, TEvent.EventVersion, title, producer, consumer, consumerName,
            outbox, inbox, handler, transportConsumer, domainEventMapping);
}
