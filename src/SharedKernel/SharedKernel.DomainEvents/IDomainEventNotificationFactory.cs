using SharedKernel.Domain;
using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents;

/// <summary>
/// Creates mediator notifications that preserve the concrete domain-event type.
/// </summary>
public interface IDomainEventNotificationFactory
{
    /// <summary>
    /// Creates a notification for the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to wrap.</param>
    /// <returns>A mediator notification for the concrete domain-event type.</returns>
    INotification Create(IDomainEvent domainEvent);
}
