using SharedKernel.Domain;
using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents;

/// <summary>
/// Dispatches domain events through the SharedKernel mediator publisher.
/// </summary>
public sealed class MediatorDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher publisher;
    private readonly IDomainEventNotificationFactory notificationFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorDomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="publisher">The mediator publisher used to publish adapter notifications.</param>
    public MediatorDomainEventDispatcher(IPublisher publisher)
        : this(publisher, UnsupportedDomainEventNotificationFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorDomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="publisher">The mediator publisher used to publish adapter notifications.</param>
    /// <param name="notificationFactory">The factory used to create concrete domain-event notifications.</param>
    public MediatorDomainEventDispatcher(IPublisher publisher, IDomainEventNotificationFactory notificationFactory)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(notificationFactory);

        this.publisher = publisher;
        this.notificationFactory = notificationFactory;
    }

    /// <inheritdoc />
    public ValueTask Dispatch<TDomainEvent>(TDomainEvent domainEvent, CancellationToken ct)
        where TDomainEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return publisher.Publish(new DomainEventNotification<TDomainEvent>(domainEvent), ct);
    }

    /// <inheritdoc />
    public ValueTask Dispatch(IDomainEvent domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return publisher.Publish(notificationFactory.Create(domainEvent), ct);
    }
}
