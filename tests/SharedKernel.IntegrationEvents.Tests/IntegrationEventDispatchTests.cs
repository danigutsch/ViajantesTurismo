using SharedKernel.Mediator;

namespace SharedKernel.IntegrationEvents.Tests;

public sealed class IntegrationEventDispatchTests
{
    [Fact]
    public async Task Dispatch_forwards_integration_events_to_the_mediator_publisher()
    {
        var publisher = new CapturingPublisher();
        var dispatcher = new MediatorIntegrationEventDispatcher(publisher);
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "tour-created");

        await dispatcher.Dispatch(integrationEvent, CancellationToken.None);

        Assert.Same(integrationEvent, publisher.Notification);
    }

    [Fact]
    public void Integration_event_handlers_are_mediator_notification_handlers()
    {
        TestIntegrationEventHandler handler = new();

        Assert.IsAssignableFrom<INotificationHandler<TestIntegrationEvent>>(handler);
    }

    [Fact]
    public void Integration_events_expose_type_version_and_idempotency_metadata()
    {
        var eventId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var integrationEvent = new TestIntegrationEvent(eventId, occurredAt, "tour-created");

        Assert.Equal("admin.tour.created", TestIntegrationEvent.EventType);
        Assert.Equal(1, TestIntegrationEvent.EventVersion);
        Assert.Equal(eventId, integrationEvent.EventId);
        Assert.Equal(occurredAt, integrationEvent.OccurredAt);
    }
}
