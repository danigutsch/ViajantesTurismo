using ViajantesTurismo.Admin.Application.Tours;

namespace ViajantesTurismo.Admin.Testing.Fakes;

public sealed class NoOpTourCapacityMutationLock : ITourCapacityMutationLock
{
    public ValueTask<IAsyncDisposable> Acquire(Guid tourId, CancellationToken ct) =>
        ValueTask.FromResult<IAsyncDisposable>(NoOpLease.Instance);

    private sealed class NoOpLease : IAsyncDisposable
    {
        public static NoOpLease Instance { get; } = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
