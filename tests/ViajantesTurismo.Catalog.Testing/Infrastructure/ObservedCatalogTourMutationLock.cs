using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class ObservedCatalogTourMutationLock(ICatalogTourMutationLock inner) : ICatalogTourMutationLock
{
    private readonly TaskCompletionSource acquisitionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task AcquisitionStarted => acquisitionStarted.Task;

    public ValueTask<IAsyncDisposable> Acquire(IReadOnlyCollection<Guid> catalogTourIds, CancellationToken ct)
    {
        acquisitionStarted.TrySetResult();
        return inner.Acquire(catalogTourIds, ct);
    }
}
