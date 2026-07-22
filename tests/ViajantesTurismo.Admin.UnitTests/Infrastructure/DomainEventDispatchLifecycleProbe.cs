using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class DomainEventDispatchLifecycleProbe
{
    private readonly List<object> createdHandlers = [];
    private readonly List<(object Handler, IDomainEvent DomainEvent, DbContext? CurrentContext)> handledEvents = [];
    private readonly List<(object Handler, DbContext? CurrentContext)> disposedHandlers = [];

    public IReadOnlyList<object> CreatedHandlers => createdHandlers;

    public IReadOnlyList<(object Handler, IDomainEvent DomainEvent, DbContext? CurrentContext)> HandledEvents => handledEvents;

    public IReadOnlyList<(object Handler, DbContext? CurrentContext)> DisposedHandlers => disposedHandlers;

    public Exception? DispatchFailure { get; init; }

    public CancellationTokenSource? CancellationSource { get; init; }

    public void RecordCreated(object handler) => createdHandlers.Add(handler);

    public void RecordHandled(object handler, IDomainEvent domainEvent, DbContext? currentContext) =>
        handledEvents.Add((handler, domainEvent, currentContext));

    public void RecordDisposed(object handler, DbContext? currentContext) =>
        disposedHandlers.Add((handler, currentContext));
}
