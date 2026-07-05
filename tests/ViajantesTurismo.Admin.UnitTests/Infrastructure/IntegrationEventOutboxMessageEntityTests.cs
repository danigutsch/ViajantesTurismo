using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Contracts.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

public sealed class IntegrationEventOutboxMessageEntityTests
{
    [Fact]
    public void Creates_message_with_envelope_and_enqueue_time()
    {
        var id = Guid.CreateVersion7();
        var occurredAt = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var enqueuedAt = occurredAt.AddSeconds(1);
        var envelope = new EventEnvelope(
            Guid.CreateVersion7(),
            AdminTourCreatedIntegrationEvent.EventType,
            AdminTourCreatedIntegrationEvent.EventVersion,
            occurredAt,
            "{}");

        var message = new IntegrationEventOutboxMessageEntity(id, envelope, enqueuedAt);

        message.Id.ShouldBe(id);
        message.Envelope.ShouldBeSameAs(envelope);
        message.EnqueuedAt.ShouldBe(enqueuedAt);
        message.PublishedAt.ShouldBeNull();
    }

    [Fact]
    public void Rejects_empty_message_id()
    {
        var envelope = new EventEnvelope(
            Guid.CreateVersion7(),
            AdminTourCreatedIntegrationEvent.EventType,
            AdminTourCreatedIntegrationEvent.EventVersion,
            DateTimeOffset.UtcNow,
            "{}");

        Action create = () =>
        {
            _ = new IntegrationEventOutboxMessageEntity(Guid.Empty, envelope, DateTimeOffset.UtcNow);
        };

        var exception = create.ShouldThrow<ArgumentException>();

        exception.ParamName.ShouldBe("id");
    }

    [Fact]
    public void Rejects_null_envelope()
    {
        Action create = () =>
        {
            _ = new IntegrationEventOutboxMessageEntity(Guid.CreateVersion7(), null!, DateTimeOffset.UtcNow);
        };

        var exception = create.ShouldThrow<ArgumentNullException>();

        exception.ParamName.ShouldBe("envelope");
    }

    [Fact]
    public void MarkPublished_sets_publication_time()
    {
        var occurredAt = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var publishedAt = occurredAt.AddMinutes(1);
        var envelope = new EventEnvelope(
            Guid.CreateVersion7(),
            AdminTourCreatedIntegrationEvent.EventType,
            AdminTourCreatedIntegrationEvent.EventVersion,
            occurredAt,
            "{}");
        var message = new IntegrationEventOutboxMessageEntity(Guid.CreateVersion7(), envelope, occurredAt);

        message.MarkPublished(publishedAt);

        message.PublishedAt.ShouldBe(publishedAt);
    }
}
