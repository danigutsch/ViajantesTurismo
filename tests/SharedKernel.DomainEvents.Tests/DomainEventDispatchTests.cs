using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents.Tests;

public sealed class DomainEventDispatchTests
{
    [Fact]
    public async Task Dispatch_forwards_domain_events_to_the_mediator_publisher()
    {
        var publisher = new CapturingPublisher();
        var dispatcher = new MediatorDomainEventDispatcher(publisher);
        var domainEvent = new TestDomainEvent("tour-created");

        await dispatcher.Dispatch(domainEvent, CancellationToken.None);

        var notification = Assert.IsType<DomainEventNotification<TestDomainEvent>>(publisher.Notification);
        Assert.Same(domainEvent, notification.DomainEvent);
    }

    [Fact]
    public void Domain_events_do_not_implement_mediator_notifications()
    {
        var domainEvent = new TestDomainEvent("tour-created");

        Assert.IsNotAssignableFrom<INotification>(domainEvent);
    }

    [Fact]
    public async Task Notification_handler_adapter_invokes_the_domain_event_handler()
    {
        var handler = new TestDomainEventHandler();
        var adapter = new DomainEventNotificationHandler<TestDomainEvent>(handler);
        var domainEvent = new TestDomainEvent("tour-created");

        await adapter.Handle(new DomainEventNotification<TestDomainEvent>(domainEvent), CancellationToken.None);

        Assert.Same(domainEvent, handler.HandledEvent);
    }
}
