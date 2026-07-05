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
        return Record(domainEvent, ct);
    }

    public virtual ValueTask Dispatch(IDomainEvent domainEvent, CancellationToken ct)
    {
        return Record(domainEvent, ct);
    }

    private ValueTask Record(IDomainEvent domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ct.ThrowIfCancellationRequested();
        _dispatchedEvents.Add(domainEvent);

        return ValueTask.CompletedTask;
    }
}
