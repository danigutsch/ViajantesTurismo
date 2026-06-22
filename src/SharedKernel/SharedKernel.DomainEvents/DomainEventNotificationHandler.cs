using SharedKernel.Domain;
using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents;

/// <summary>
/// Adapts a typed domain event handler to the mediator notification handler contract.
/// </summary>
/// <typeparam name="TDomainEvent">The wrapped domain event type.</typeparam>
public sealed class DomainEventNotificationHandler<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent
{
    private readonly IDomainEventHandler<TDomainEvent> handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventNotificationHandler{TDomainEvent}"/> class.
    /// </summary>
    /// <param name="handler">The typed domain event handler to invoke.</param>
    public DomainEventNotificationHandler(IDomainEventHandler<TDomainEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        this.handler = handler;
    }

    /// <inheritdoc />
    public ValueTask Handle(DomainEventNotification<TDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return handler.Handle(notification.DomainEvent, ct);
    }
}
