using SharedKernel.Domain;
using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents;

internal sealed class UnsupportedDomainEventNotificationFactory : IDomainEventNotificationFactory
{
    public static UnsupportedDomainEventNotificationFactory Instance { get; } = new();

    public INotification Create(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        throw new NotSupportedException($"Domain event type '{domainEvent.GetType().FullName}' is not configured for non-generic mediator dispatch.");
    }
}
