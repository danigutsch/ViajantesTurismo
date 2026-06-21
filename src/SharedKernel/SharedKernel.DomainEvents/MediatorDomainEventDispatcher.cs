using SharedKernel.Domain;
using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents;

/// <summary>
/// Dispatches domain events through the SharedKernel mediator publisher.
/// </summary>
public sealed class MediatorDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher publisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorDomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="publisher">The mediator publisher used to publish adapter notifications.</param>
    public MediatorDomainEventDispatcher(IPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        this.publisher = publisher;
    }

    /// <inheritdoc />
    public ValueTask Dispatch<TDomainEvent>(TDomainEvent domainEvent, CancellationToken ct)
        where TDomainEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return publisher.Publish(new DomainEventNotification<TDomainEvent>(domainEvent), ct);
    }
}
