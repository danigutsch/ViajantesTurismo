using SharedKernel.Domain;
using SharedKernel.DomainEvents;

namespace ViajantesTurismo.Admin.Testing.Fakes;

public class CapturingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly List<IDomainEvent> _dispatchedEvents = [];

    public IReadOnlyList<IDomainEvent> DispatchedEvents => _dispatchedEvents;

    public virtual ValueTask Dispatch<TDomainEvent>(TDomainEvent domainEvent, CancellationToken ct)
        where TDomainEvent : IDomainEvent
    {
        ct.ThrowIfCancellationRequested();
        _dispatchedEvents.Add(domainEvent);

        return ValueTask.CompletedTask;
    }
}
