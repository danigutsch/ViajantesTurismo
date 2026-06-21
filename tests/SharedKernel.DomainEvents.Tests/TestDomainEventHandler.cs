namespace SharedKernel.DomainEvents.Tests;

internal sealed class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent>
{
    public TestDomainEvent? HandledEvent { get; private set; }

    public ValueTask Handle(TestDomainEvent domainEvent, CancellationToken ct)
    {
        HandledEvent = domainEvent;

        return ValueTask.CompletedTask;
    }
}
