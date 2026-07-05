using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.CloudEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

public sealed class IntegrationEventOutboxMessageTests
{
    private const string JsonContentType = "application/json";

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
            CloudEventConstants.Spec,
            CloudEventConstants.SpecVersion,
            eventId.ToString("D"),
            source,
            AdminTourCreatedIntegrationEvent.EventType,
            AdminTourCreatedIntegrationEvent.EventVersion,
            occurredAt,
            "tour-1",
            JsonContentType,
            new Uri("https://schemas.example/admin-tour-created.json"),
            "{}",
            EventPayloadEncoding.Json,
            "{\"traceparent\":\"00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00\"}",
            enqueuedAt);

        message.Id.ShouldBe(id);
        message.EnvelopeSpec.ShouldBe(CloudEventConstants.Spec);
        message.EnvelopeSpecVersion.ShouldBe(CloudEventConstants.SpecVersion);
        message.EventId.ShouldBe(eventId.ToString("D"));
        message.Source.ShouldBe(source);
        message.EventType.ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        message.EventVersion.ShouldBe(AdminTourCreatedIntegrationEvent.EventVersion);
        message.Time.ShouldBe(occurredAt);
        message.Subject.ShouldBe("tour-1");
        message.DataContentType.ShouldBe(JsonContentType);
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
                CloudEventConstants.Spec,
                CloudEventConstants.SpecVersion,
                Guid.CreateVersion7().ToString("D"),
                new Uri("urn:viajantes:admin"),
                AdminTourCreatedIntegrationEvent.EventType,
                AdminTourCreatedIntegrationEvent.EventVersion,
                DateTimeOffset.UtcNow,
                null,
                JsonContentType,
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
            CloudEventConstants.Spec,
            CloudEventConstants.SpecVersion,
            Guid.CreateVersion7().ToString("D"),
            new Uri("urn:viajantes:admin"),
            AdminTourCreatedIntegrationEvent.EventType,
            AdminTourCreatedIntegrationEvent.EventVersion,
            occurredAt,
            null,
            JsonContentType,
            null,
            "{}",
            EventPayloadEncoding.Json,
            null,
            occurredAt);

        message.MarkPublished(publishedAt);

        message.PublishedAt.ShouldBe(publishedAt);
    }

    [Fact]
    public async Task Enqueue_adds_current_trace_extensions_when_activity_exists()
    {
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher();
        var dbContext = scope.DbContext;
        var outbox = new EfIntegrationEventOutbox<AdminWriteDbContext>(
            dbContext,
            new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)),
            new AdminIntegrationEventSerializer());
        using var activity = new Activity("outbox-test");
        activity.TraceStateString = "vendor=value";
        activity.Start();

        await outbox.Enqueue(new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            new DateTimeOffset(2026, 6, 22, 11, 59, 0, TimeSpan.Zero),
            Guid.CreateVersion7(),
            "andes-trace-2026",
            "Andes Trace 2026"), CancellationToken.None);
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var outboxMessage = dbContext.Set<IntegrationEventOutboxMessage>().ShouldHaveSingleItem();
        outboxMessage.ExtensionAttributesJson.ShouldNotBeNull();
        outboxMessage.ExtensionAttributesJson.ShouldContain("traceparent", StringComparison.Ordinal);
        outboxMessage.ExtensionAttributesJson.ShouldContain("tracestate", StringComparison.Ordinal);
        outboxMessage.ExtensionAttributesJson.ShouldContain("vendor=value", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Enqueue_requires_current_save_changes_context_when_no_context_was_constructed()
    {
        var outbox = new EfIntegrationEventOutbox<AdminWriteDbContext>(
            new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)),
            new AdminIntegrationEventSerializer());

        Func<Task> enqueue = async () => await outbox.Enqueue(new AdminTourCreatedIntegrationEvent(
                Guid.CreateVersion7(),
                new DateTimeOffset(2026, 6, 22, 11, 59, 0, TimeSpan.Zero),
                Guid.CreateVersion7(),
                "andes-no-context-2026",
                "Andes No Context 2026"), CancellationToken.None)
            .ConfigureAwait(false);

        var exception = await enqueue.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldContain("No current AdminWriteDbContext SaveChanges context", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Relay_publishes_pending_message_and_marks_it_published()
    {
        var publisher = new RecordingEventEnvelopePublisher();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(publisher, timeProvider);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-relay-2026");

        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        await relay.PublishPending(1, TestContext.Current.CancellationToken);

        publisher.Published.Count.ShouldBe(1);
        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishedAt.ShouldBe(timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task Relay_records_retry_state_when_publish_fails()
    {
        var publisher = new RecordingEventEnvelopePublisher
        {
            Failure = new InvalidOperationException("transport unavailable")
        };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(publisher, timeProvider);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-retry-2026");

        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        await relay.PublishPending(1, TestContext.Current.CancellationToken);

        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishedAt.ShouldBeNull();
        message.PublishAttempts.ShouldBe(1);
        message.LastPublishError.ShouldContain("transport unavailable", StringComparison.Ordinal);
        ((IRetryableMessage)message).Attempts.ShouldBe(1);
    }
}
