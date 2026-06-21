using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents.Tests;

internal sealed class CapturingPublisher : IPublisher
{
    public object? Notification { get; private set; }

    public ValueTask Publish<TNotification>(TNotification notification, CancellationToken ct)
        where TNotification : INotification
    {
        Notification = notification;

        return ValueTask.CompletedTask;
    }
}
