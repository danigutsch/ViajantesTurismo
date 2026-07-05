using SharedKernel.Domain;
using SharedKernel.Mediator;

namespace SharedKernel.DomainEvents.Tests;

internal sealed class TestDomainEventNotificationFactory : IDomainEventNotificationFactory
{
    public INotification Create(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            TestDomainEvent typedDomainEvent => new DomainEventNotification<TestDomainEvent>(typedDomainEvent),
            _ => throw new NotSupportedException($"Domain event type '{domainEvent.GetType().FullName}' is not configured for non-generic mediator dispatch."),
        };
    }
}
