using Microsoft.EntityFrameworkCore;
using PandaBear.Domain;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Records handler creation, handling and disposal during domain event dispatch tests.
/// </summary>
internal sealed class DomainEventDispatchLifecycleProbe
{
    private readonly List<object> createdHandlers = [];
    private readonly List<(object Handler, IDomainEvent DomainEvent, DbContext? CurrentContext)> handledEvents = [];
    private readonly List<(object Handler, DbContext? CurrentContext)> disposedHandlers = [];

    /// <summary>
    /// Gets the handlers that were created.
    /// </summary>
    public IReadOnlyList<object> CreatedHandlers => createdHandlers;

    /// <summary>
    /// Gets each handled event with its handler and the current context at handling time.
    /// </summary>
    public IReadOnlyList<(object Handler, IDomainEvent DomainEvent, DbContext? CurrentContext)> HandledEvents => handledEvents;

    /// <summary>
    /// Gets the disposed handlers with the current context at disposal time.
    /// </summary>
    public IReadOnlyList<(object Handler, DbContext? CurrentContext)> DisposedHandlers => disposedHandlers;

    /// <summary>
    /// Gets the exception handlers should throw, or <see langword="null"/> for no failure.
    /// </summary>
    public Exception? DispatchFailure
    {
        get; init;
    }

    /// <summary>
    /// Gets the source handlers should cancel, or <see langword="null"/> for no cancellation.
    /// </summary>
    public CancellationTokenSource? CancellationSource
    {
        get; init;
    }

    /// <summary>
    /// Records that <paramref name="handler"/> was created.
    /// </summary>
    public void RecordCreated(object handler) => createdHandlers.Add(handler);

    /// <summary>
    /// Records that <paramref name="handler"/> handled <paramref name="domainEvent"/> under <paramref name="currentContext"/>.
    /// </summary>
    public void RecordHandled(object handler, IDomainEvent domainEvent, DbContext? currentContext) =>
        handledEvents.Add((handler, domainEvent, currentContext));

    /// <summary>
    /// Records that <paramref name="handler"/> was disposed under <paramref name="currentContext"/>.
    /// </summary>
    public void RecordDisposed(object handler, DbContext? currentContext) =>
        disposedHandlers.Add((handler, currentContext));
}
