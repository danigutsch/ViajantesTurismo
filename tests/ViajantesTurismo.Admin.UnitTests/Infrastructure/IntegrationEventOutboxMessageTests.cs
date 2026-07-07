using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.CloudEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.OutboxCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventRelayCapability)]
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
    public void Relay_options_reject_non_positive_batch_size()
    {
        // Arrange
        var validator = new IntegrationEventOutboxRelayOptionsValidator();
        var options = new IntegrationEventOutboxRelayOptions
        {
            BatchSize = 0
        };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("batch size", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Relay_options_reject_non_positive_poll_interval()
    {
        // Arrange
        var validator = new IntegrationEventOutboxRelayOptionsValidator();
        var options = new IntegrationEventOutboxRelayOptions
        {
            PollInterval = TimeSpan.Zero
        };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("poll interval", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Relay_options_reject_non_positive_claim_lease_duration()
    {
        // Arrange
        var validator = new IntegrationEventOutboxRelayOptionsValidator();
        var options = new IntegrationEventOutboxRelayOptions
        {
            ClaimLeaseDuration = TimeSpan.Zero
        };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("claim lease duration", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Enqueue_adds_current_trace_extensions_when_activity_exists()
    {
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher();
        var dbContext = scope.DbContext;
        var outbox = new EfIntegrationEventOutbox<AdminWriteDbContext>(
            dbContext,
            new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)),
            RegisteredIntegrationEventSerializerTestServices.CreateSerializer());
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
    public async Task Relay_publishes_pending_message_and_marks_it_published()
    {
        // Arrange
        var publisher = new RecordingEventEnvelopePublisher();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(publisher, timeProvider);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-relay-2026");

        // Act
        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var publishedCount = await relay.PublishPending(1, TestContext.Current.CancellationToken);

        // Assert
        publishedCount.ShouldBe(1);
        publisher.Published.Count.ShouldBe(1);
        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishedAt.ShouldBe(timeProvider.GetUtcNow());
        message.ClaimedBy.ShouldBeNull();
        message.ClaimedUntil.ShouldBeNull();
    }

    [Fact]
    public async Task Relay_records_retry_state_when_publish_fails()
    {
        // Arrange
        var publisher = new RecordingEventEnvelopePublisher
        {
            Failure = new InvalidOperationException("transport unavailable")
        };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(publisher, timeProvider);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-retry-2026");

        // Act
        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var publishedCount = await relay.PublishPending(1, TestContext.Current.CancellationToken);

        // Assert
        publishedCount.ShouldBe(1);
        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishedAt.ShouldBeNull();
        message.PublishAttempts.ShouldBe(1);
        message.LastPublishError.ShouldContain("transport unavailable", StringComparison.Ordinal);
        message.ClaimedBy.ShouldBeNull();
        message.ClaimedUntil.ShouldBeNull();
        ((IRetryableMessage)message).Attempts.ShouldBe(1);
    }

    [Fact]
    public async Task Relay_skips_message_with_active_claim()
    {
        // Arrange
        var publisher = new RecordingEventEnvelopePublisher();
        var now = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(publisher, timeProvider);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-claimed-2026");
        AdminWriteDbContextTestFactory.ClaimSingleOutboxMessage(provider, now.AddMinutes(5));

        // Act
        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var publishedCount = await relay.PublishPending(1, TestContext.Current.CancellationToken);

        // Assert
        publishedCount.ShouldBe(0);
        publisher.Published.Count.ShouldBe(0);
        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishedAt.ShouldBeNull();
        message.ClaimedBy.ShouldBe("test-relay");
        message.ClaimedUntil.ShouldBe(now.AddMinutes(5));
    }

    [Fact]
    public async Task Relay_reclaims_message_when_claim_lease_expired()
    {
        // Arrange
        var publisher = new RecordingEventEnvelopePublisher();
        var now = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(publisher, timeProvider);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-expired-claim-2026");
        AdminWriteDbContextTestFactory.ClaimSingleOutboxMessage(provider, now.AddSeconds(-1));

        // Act
        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var publishedCount = await relay.PublishPending(1, TestContext.Current.CancellationToken);

        // Assert
        publishedCount.ShouldBe(1);
        publisher.Published.Count.ShouldBe(1);
        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishedAt.ShouldBe(now);
        message.ClaimedBy.ShouldBeNull();
        message.ClaimedUntil.ShouldBeNull();
    }

    [Fact]
    public async Task Concurrent_relays_publish_once_when_claim_strategy_is_atomic()
    {
        // Arrange
        var publisher = new RecordingEventEnvelopePublisher();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(
            publisher,
            timeProvider,
            configureServices: services =>
            {
                services.Replace(
                    ServiceDescriptor.Singleton<
                        IIntegrationEventOutboxClaimStrategy<AdminWriteDbContext>,
                        SingleClaimIntegrationEventOutboxClaimStrategy<AdminWriteDbContext>>());
                services.Replace(
                    ServiceDescriptor.Transient<
                        EfIntegrationEventOutboxRelay<AdminWriteDbContext>,
                        EfIntegrationEventOutboxRelay<AdminWriteDbContext>>());
            });
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-atomic-claim-2026");
        var firstRelay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var secondRelay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();

        secondRelay.ShouldNotBeSameAs(firstRelay);

        // Act
        var publishTasks = new[]
        {
            firstRelay.PublishPending(1, TestContext.Current.CancellationToken).AsTask(),
            secondRelay.PublishPending(1, TestContext.Current.CancellationToken).AsTask(),
        };
        var publishedCounts = await Task.WhenAll(publishTasks);

        // Assert
        var totalPublished = publishedCounts.Sum();
        totalPublished.ShouldBe(1);
        publisher.Published.Count.ShouldBe(1);
        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishedAt.ShouldBe(timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task PostgreSql_atomic_claim_sql_uses_skip_locked()
    {
        // Arrange
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher();
        var entityType = scope.DbContext.Model.FindEntityType(typeof(IntegrationEventOutboxMessage));
        entityType.ShouldNotBeNull();

        // Act
        var sql = PostgreSqlIntegrationEventOutboxClaimStrategy<AdminWriteDbContext>.CreateClaimSql(entityType);

        // Assert
        sql.ShouldContain("WITH claimed AS", StringComparison.Ordinal);
        sql.ShouldContain("UPDATE \"messaging\".\"outbox_messages\" AS message", StringComparison.Ordinal);
        sql.ShouldContain("FOR UPDATE SKIP LOCKED", StringComparison.Ordinal);
        sql.ShouldContain("RETURNING *", StringComparison.Ordinal);
        sql.ShouldContain("SELECT *", StringComparison.Ordinal);
        sql.ShouldContain("FROM claimed", StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostgreSql_transport_claim_sql_uses_skip_locked_and_consumer_filter()
    {
        // Arrange
        await using var scenario = AdminWriteDbContextTestFactory.CreateTransportScenario();
        var dbContext = scenario.DbContext;
        var entityType = dbContext.Model.FindEntityType(typeof(IntegrationEventTransportMessage));
        entityType.ShouldNotBeNull();

        // Act
        var sql = PostgreSqlIntegrationEventTransportClaimSql.CreateClaimSql(entityType);

        // Assert
        sql.ShouldContain("WITH claimed AS", StringComparison.Ordinal);
        sql.ShouldContain("UPDATE \"messaging\".\"transport_messages\" AS message", StringComparison.Ordinal);
        sql.ShouldContain("candidate.\"ConsumerName\" = {0}", StringComparison.Ordinal);
        sql.ShouldContain("FOR UPDATE SKIP LOCKED", StringComparison.Ordinal);
        sql.ShouldContain("RETURNING *", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Transport_producer_persists_envelope_for_catalog_consumer()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        await using var scenario = AdminWriteDbContextTestFactory.CreateTransportScenario(new FakeTimeProvider(now));
        var dbContext = scenario.DbContext;
        var publisher = scenario.Publisher;
        var envelope = AdminWriteDbContextTestFactory.CreateEnvelope("transport-producer-event");

        // Act
        await publisher.Publish(envelope, TestContext.Current.CancellationToken);
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var message = dbContext.Set<IntegrationEventTransportMessage>().ShouldHaveSingleItem();
        message.ConsumerName.ShouldBe(IntegrationEventConsumerNames.Catalog);
        message.EventId.ShouldBe(envelope.EventId);
        message.EventType.ShouldBe(envelope.EventType);
        message.Payload.ShouldBe(envelope.Payload);
        message.ReceivedAt.ShouldBe(now);
        message.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Transport_model_uses_consumer_and_event_id_as_inbox_duplicate_key()
    {
        // Arrange
        await using var scenario = AdminWriteDbContextTestFactory.CreateTransportScenario();
        var dbContext = scenario.DbContext;
        var entityType = dbContext.Model.FindEntityType(typeof(IntegrationEventTransportMessage)).ShouldNotBeNull();

        // Act
        var index = entityType.GetIndexes()
            .SingleOrDefault(candidate => candidate.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(IntegrationEventTransportMessage.ConsumerName), nameof(IntegrationEventTransportMessage.EventId)]));

        // Assert
        index.ShouldNotBeNull();
        index.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void PostgreSql_transport_consumer_registration_adds_hosted_consumer_service()
    {
        // Act
        var hostedService = AdminWriteDbContextTestFactory.GetPostgreSqlTransportConsumerHostedServiceDescriptor();

        // Assert
        hostedService.ImplementationType.ShouldBe(typeof(PostgreSqlIntegrationEventTransportConsumerHostedService<AdminWriteDbContext>));
    }

    [Fact]
    public async Task PostgreSql_atomic_claim_rejects_non_postgresql_provider()
    {
        // Arrange
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher();
        var strategy = new PostgreSqlIntegrationEventOutboxClaimStrategy<AdminWriteDbContext>();
        Func<Task> claim = async () => await strategy.ClaimPending(
            scope.DbContext,
            1,
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            "test-relay",
            new DateTimeOffset(2026, 6, 22, 12, 5, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken);

        // Act
        var exception = await claim.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);
    }

    [Fact]
    public void PostgreSql_atomic_claim_registration_replaces_default_claim_strategy()
    {
        // Arrange

        // Act
        var descriptor = AdminWriteDbContextTestFactory.GetPostgreSqlOutboxRelayClaimStrategyDescriptor();

        // Assert
        descriptor.ImplementationType.ShouldBe(typeof(PostgreSqlIntegrationEventOutboxClaimStrategy<AdminWriteDbContext>));
    }

    [Fact]
    public async Task Relay_uses_configured_batch_size_when_no_explicit_size_is_passed()
    {
        // Arrange
        var publisher = new RecordingEventEnvelopePublisher();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(
            publisher,
            timeProvider,
            options => options.BatchSize = 1);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-batch-1-2026");
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-batch-2-2026");

        // Act
        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var publishedCount = await relay.PublishPending(TestContext.Current.CancellationToken);

        // Assert
        publishedCount.ShouldBe(1);
        publisher.Published.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Relay_records_exception_type_when_publish_failure_message_is_empty()
    {
        // Arrange
        var publisher = new RecordingEventEnvelopePublisher
        {
            Failure = new InvalidOperationException(string.Empty)
        };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(publisher, timeProvider);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-empty-error-2026");

        // Act
        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var publishedCount = await relay.PublishPending(1, TestContext.Current.CancellationToken);

        // Assert
        publishedCount.ShouldBe(1);
        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishAttempts.ShouldBe(1);
        message.LastPublishError.ShouldBe(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public async Task Relay_truncates_long_publish_failure_message_to_configured_column_length()
    {
        // Arrange
        var publisher = new RecordingEventEnvelopePublisher
        {
            Failure = new InvalidOperationException(new string('x', 2_100))
        };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
        await using var provider = AdminWriteDbContextTestFactory.CreateOutboxRelayProvider(publisher, timeProvider);
        await AdminWriteDbContextTestFactory.EnqueueAdminTourCreatedIntegrationEvent(provider, "andes-long-error-2026");

        // Act
        var relay = provider.GetRequiredService<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var publishedCount = await relay.PublishPending(1, TestContext.Current.CancellationToken);

        // Assert
        publishedCount.ShouldBe(1);
        var message = AdminWriteDbContextTestFactory.GetSingleOutboxMessage(provider);
        message.PublishAttempts.ShouldBe(1);
        message.LastPublishError.ShouldNotBeNull();
        message.LastPublishError.Length.ShouldBe(IntegrationEventOutboxMessage.LastPublishErrorMaxLength);
    }

}
