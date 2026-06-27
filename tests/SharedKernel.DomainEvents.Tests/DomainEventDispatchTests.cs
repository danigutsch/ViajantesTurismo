using SharedKernel.Mediator;

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
        var notification = Assert.IsType<DomainEventNotification<TestDomainEvent>>(publisher.Notification);
        Assert.Same(domainEvent, notification.DomainEvent);
        Assert.Equal(cancellationTokenSource.Token, publisher.CancellationToken);
    }

    [Fact]
    public void Dispatcher_constructor_rejects_null_publisher()
    {
        // Arrange
        var constructor = typeof(MediatorDomainEventDispatcher).GetConstructor([typeof(IPublisher)]);
        Assert.NotNull(constructor);

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => constructor.Invoke([null]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("publisher", argumentException.ParamName);
    }

    [Fact]
    public void Dispatch_rejects_null_domain_events()
    {
        // Arrange
        var dispatcher = new MediatorDomainEventDispatcher(new CapturingPublisher());
        var method = typeof(MediatorDomainEventDispatcher).GetMethod(nameof(MediatorDomainEventDispatcher.Dispatch));
        Assert.NotNull(method);
        var genericMethod = method.MakeGenericMethod(typeof(TestDomainEvent));

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => genericMethod.Invoke(dispatcher, [null, CancellationToken.None]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("domainEvent", argumentException.ParamName);
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
        // Arrange
        var handler = new TestDomainEventHandler();
        var adapter = new DomainEventNotificationHandler<TestDomainEvent>(handler);
        var domainEvent = new TestDomainEvent("tour-created");
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await adapter.Handle(new DomainEventNotification<TestDomainEvent>(domainEvent), cancellationTokenSource.Token);

        // Assert
        Assert.Same(domainEvent, handler.HandledEvent);
        Assert.Equal(cancellationTokenSource.Token, handler.CancellationToken);
    }

    [Fact]
    public void Domain_event_notification_rejects_null_domain_events()
    {
        // Arrange
        var constructor = typeof(DomainEventNotification<TestDomainEvent>).GetConstructor([typeof(TestDomainEvent)]);
        Assert.NotNull(constructor);

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => constructor.Invoke([null]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("domainEvent", argumentException.ParamName);
    }

    [Fact]
    public void Notification_handler_rejects_null_handlers()
    {
        // Arrange
        var constructor = typeof(DomainEventNotificationHandler<TestDomainEvent>).GetConstructor([typeof(IDomainEventHandler<TestDomainEvent>)]);
        Assert.NotNull(constructor);

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => constructor.Invoke([null]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("handler", argumentException.ParamName);
    }

    [Fact]
    public void Notification_handler_rejects_null_notifications()
    {
        // Arrange
        var adapter = new DomainEventNotificationHandler<TestDomainEvent>(new TestDomainEventHandler());
        var method = typeof(DomainEventNotificationHandler<TestDomainEvent>).GetMethod(nameof(DomainEventNotificationHandler<TestDomainEvent>.Handle));
        Assert.NotNull(method);

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(adapter, [null, CancellationToken.None]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("notification", argumentException.ParamName);
    }
}
