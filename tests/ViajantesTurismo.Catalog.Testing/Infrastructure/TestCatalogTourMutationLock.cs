using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestCatalogTourMutationLock : ICatalogTourMutationLock
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> gates = new();

    public async ValueTask<IAsyncDisposable> Acquire(IReadOnlyCollection<Guid> catalogTourIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(catalogTourIds);
        var acquired = new List<SemaphoreSlim>();
        try
        {
            foreach (var id in catalogTourIds.Distinct().Order())
            {
                var gate = gates.GetOrAdd(id, static _ => new SemaphoreSlim(1, 1));
                await gate.WaitAsync(ct).ConfigureAwait(false);
                acquired.Add(gate);
            }

            return new Lease(acquired);
        }
        catch
        {
            foreach (var gate in acquired)
            {
                gate.Release();
            }

            throw;
        }
    }

    private sealed class Lease(IReadOnlyList<SemaphoreSlim> gates) : IAsyncDisposable
    {
        private int disposed;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                foreach (var gate in gates.Reverse())
                {
                    gate.Release();
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}
