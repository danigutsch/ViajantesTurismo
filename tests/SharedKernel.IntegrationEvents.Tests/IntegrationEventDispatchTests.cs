using SharedKernel.Mediator;

namespace SharedKernel.IntegrationEvents.Tests;

public sealed class IntegrationEventDispatchTests
{
    [Fact]
    public async Task Dispatch_forwards_integration_events_to_the_mediator_publisher()
    {
        // Arrange
        var publisher = new CapturingPublisher();
        var dispatcher = new MediatorIntegrationEventDispatcher(publisher);
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "tour-created");
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await dispatcher.Dispatch(integrationEvent, cancellationTokenSource.Token);

        // Assert
        Assert.Same(integrationEvent, publisher.Notification);
        Assert.Equal(cancellationTokenSource.Token, publisher.CancellationToken);
    }

    [Fact]
    public void Constructor_rejects_null_publisher()
    {
        // Arrange
        var constructor = typeof(MediatorIntegrationEventDispatcher).GetConstructor([typeof(IPublisher)]);
        Assert.NotNull(constructor);

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => constructor.Invoke([null]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("publisher", argumentException.ParamName);
    }

    [Fact]
    public void Dispatch_rejects_null_integration_events()
    {
        // Arrange
        var dispatcher = new MediatorIntegrationEventDispatcher(new CapturingPublisher());
        var method = typeof(MediatorIntegrationEventDispatcher).GetMethod(nameof(MediatorIntegrationEventDispatcher.Dispatch));
        Assert.NotNull(method);
        var genericMethod = method.MakeGenericMethod(typeof(TestIntegrationEvent));

        // Act
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => genericMethod.Invoke(dispatcher, [null, CancellationToken.None]));

        // Assert
        var argumentException = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal("integrationEvent", argumentException.ParamName);
    }

    [Fact]
    public void Integration_event_handlers_are_mediator_notification_handlers()
    {
        TestIntegrationEventHandler handler = new();

        Assert.IsAssignableFrom<INotificationHandler<TestIntegrationEvent>>(handler);
    }

}
