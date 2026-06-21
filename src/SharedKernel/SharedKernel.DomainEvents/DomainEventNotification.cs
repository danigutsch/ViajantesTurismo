using SharedKernel.Domain;
using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents;

/// <summary>
/// Adapts a domain event to the mediator notification contract without coupling domain events to the mediator.
/// </summary>
/// <typeparam name="TDomainEvent">The wrapped domain event type.</typeparam>
public sealed class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventNotification{TDomainEvent}"/> class.
    /// </summary>
    /// <param name="domainEvent">The wrapped domain event.</param>
    public DomainEventNotification(TDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        DomainEvent = domainEvent;
    }

    /// <summary>
    /// Gets the wrapped domain event.
    /// </summary>
    public TDomainEvent DomainEvent { get; }
}
