using SharedKernel.Mediator;
using SharedKernel.Testing.Assertions;

namespace SharedKernel.DomainEvents.Tests;

public sealed class DomainEventDispatchTests
{
    [Fact]
    public async Task Dispatch_forwards_domain_events_to_the_mediator_publisher()
    {
        // Arrange
        var publisher = new CapturingPublisher();
        var dispatcher = new MediatorDomainEventDispatcher(publisher);
        var domainEvent = new TestDomainEvent("tour-created");
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await dispatcher.Dispatch(domainEvent, cancellationTokenSource.Token);

        // Assert
        var notification = TestAssert.IsType<DomainEventNotification<TestDomainEvent>>(publisher.Notification);
        TestAssert.Same(domainEvent, notification.DomainEvent);
        publisher.CancellationToken.ShouldBe(cancellationTokenSource.Token);
    }

    [Fact]
    public void Dispatcher_constructor_rejects_null_publisher()
    {
        // Arrange
        var constructor = typeof(MediatorDomainEventDispatcher).GetConstructor([typeof(IPublisher)]).ShouldNotBeNull();

        // Act
        var argumentException = ExceptionAssertions.ThrowsInner<ArgumentNullException>(() => constructor.Invoke([null]));
        argumentException.ParamName.ShouldBe("publisher");
    }

    [Fact]
    public void Dispatch_rejects_null_domain_events()
    {
        // Arrange
        var dispatcher = new MediatorDomainEventDispatcher(new CapturingPublisher());
        var method = typeof(MediatorDomainEventDispatcher).GetMethod(nameof(MediatorDomainEventDispatcher.Dispatch)).ShouldNotBeNull();
        var genericMethod = method.MakeGenericMethod(typeof(TestDomainEvent));

        // Act
        var argumentException = ExceptionAssertions.ThrowsInner<ArgumentNullException>(() => genericMethod.Invoke(dispatcher, [null, CancellationToken.None]));
        argumentException.ParamName.ShouldBe("domainEvent");
    }

    [Fact]
    public void Domain_events_do_not_implement_mediator_notifications()
    {
        var domainEvent = new TestDomainEvent("tour-created");

        TestAssert.IsNotAssignableFrom<INotification>(domainEvent);
    }

    [Fact]
    public async Task Notification_handler_adapter_invokes_the_domain_event_handler()
    {
        // Arrange
        var handler = new TestDomainEventHandler();
        var adapter = new DomainEventNotificationHandler<TestDomainEvent>(handler);
        var domainEvent = new TestDomainEvent("tour-created");
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await adapter.Handle(new DomainEventNotification<TestDomainEvent>(domainEvent), cancellationTokenSource.Token);

        // Assert
        TestAssert.Same(domainEvent, handler.HandledEvent);
        handler.CancellationToken.ShouldBe(cancellationTokenSource.Token);
    }

    [Fact]
    public void Domain_event_notification_rejects_null_domain_events()
    {
        // Arrange
        var constructor = typeof(DomainEventNotification<TestDomainEvent>).GetConstructor([typeof(TestDomainEvent)]).ShouldNotBeNull();

        // Act
        var argumentException = ExceptionAssertions.ThrowsInner<ArgumentNullException>(() => constructor.Invoke([null]));
        argumentException.ParamName.ShouldBe("domainEvent");
    }

    [Fact]
    public void Notification_handler_rejects_null_handlers()
    {
        // Arrange
        var constructor = typeof(DomainEventNotificationHandler<TestDomainEvent>).GetConstructor([typeof(IDomainEventHandler<TestDomainEvent>)]).ShouldNotBeNull();

        // Act
        var argumentException = ExceptionAssertions.ThrowsInner<ArgumentNullException>(() => constructor.Invoke([null]));
        argumentException.ParamName.ShouldBe("handler");
    }

    [Fact]
    public void Notification_handler_rejects_null_notifications()
    {
        // Arrange
        var adapter = new DomainEventNotificationHandler<TestDomainEvent>(new TestDomainEventHandler());
        var method = typeof(DomainEventNotificationHandler<TestDomainEvent>).GetMethod(nameof(DomainEventNotificationHandler<TestDomainEvent>.Handle)).ShouldNotBeNull();

        // Act
        var argumentException = ExceptionAssertions.ThrowsInner<ArgumentNullException>(() => method.Invoke(adapter, [null, CancellationToken.None]));
        argumentException.ParamName.ShouldBe("notification");
    }
}
