using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Contracts.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

public sealed class IntegrationEventOutboxMessageTests
{
    [Fact]
    public void Creates_message_with_envelope_fields_and_enqueue_time()
    {
        var id = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var occurredAt = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var enqueuedAt = occurredAt.AddSeconds(1);
        var source = new Uri("urn:viajantes:admin");

        var message = new IntegrationEventOutboxMessage(
            id,
            EventEnvelope.CloudEventsSpec,
            EventEnvelope.CloudEventsSpecVersion,
            eventId.ToString("D"),
            source,
            AdminTourCreatedIntegrationEvent.EventType,
            AdminTourCreatedIntegrationEvent.EventVersion,
            occurredAt,
            "tour-1",
            "application/json",
            new Uri("https://schemas.example/admin-tour-created.json"),
            "{}",
            EventPayloadEncoding.Json,
            "{\"traceparent\":\"00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00\"}",
            enqueuedAt);

        message.Id.ShouldBe(id);
        message.EnvelopeSpec.ShouldBe(EventEnvelope.CloudEventsSpec);
        message.EnvelopeSpecVersion.ShouldBe(EventEnvelope.CloudEventsSpecVersion);
        message.EventId.ShouldBe(eventId.ToString("D"));
        message.Source.ShouldBe(source);
        message.EventType.ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        message.EventVersion.ShouldBe(AdminTourCreatedIntegrationEvent.EventVersion);
        message.Time.ShouldBe(occurredAt);
        message.Subject.ShouldBe("tour-1");
        message.DataContentType.ShouldBe("application/json");
        message.DataSchema.ShouldBe(new Uri("https://schemas.example/admin-tour-created.json"));
        message.Payload.ShouldBe("{}");
        message.PayloadEncoding.ShouldBe(EventPayloadEncoding.Json);
        message.ExtensionAttributesJson.ShouldContain("traceparent", StringComparison.Ordinal);
        message.EnqueuedAt.ShouldBe(enqueuedAt);
        message.PublishedAt.ShouldBeNull();
    }

    [Fact]
    public void Rejects_empty_message_id()
    {
        Action create = () =>
        {
            _ = new IntegrationEventOutboxMessage(
                Guid.Empty,
                EventEnvelope.CloudEventsSpec,
                EventEnvelope.CloudEventsSpecVersion,
                Guid.CreateVersion7().ToString("D"),
                new Uri("urn:viajantes:admin"),
                AdminTourCreatedIntegrationEvent.EventType,
                AdminTourCreatedIntegrationEvent.EventVersion,
                DateTimeOffset.UtcNow,
                null,
                "application/json",
                null,
                "{}",
                EventPayloadEncoding.Json,
                null,
                DateTimeOffset.UtcNow);
        };

        var exception = create.ShouldThrow<ArgumentException>();

        exception.ParamName.ShouldBe("id");
    }

    [Fact]
    public void MarkPublished_sets_publication_time()
    {
        var occurredAt = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var publishedAt = occurredAt.AddMinutes(1);
        var message = new IntegrationEventOutboxMessage(
            Guid.CreateVersion7(),
            EventEnvelope.CloudEventsSpec,
            EventEnvelope.CloudEventsSpecVersion,
            Guid.CreateVersion7().ToString("D"),
            new Uri("urn:viajantes:admin"),
            AdminTourCreatedIntegrationEvent.EventType,
            AdminTourCreatedIntegrationEvent.EventVersion,
            occurredAt,
            null,
            "application/json",
            null,
            "{}",
            EventPayloadEncoding.Json,
            null,
            occurredAt);

        message.MarkPublished(publishedAt);

        message.PublishedAt.ShouldBe(publishedAt);
    }
}
