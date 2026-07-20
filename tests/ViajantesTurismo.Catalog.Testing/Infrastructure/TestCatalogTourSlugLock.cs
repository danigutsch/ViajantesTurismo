using System.Threading.Channels;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestCatalogTourSlugLock : ICatalogTourSlugLock
{
    private readonly Channel<bool> gate = CreateGate();
    private readonly List<string> acquiredSlugs = [];

    public IReadOnlyList<string> AcquiredSlugs => acquiredSlugs;

    public async ValueTask<IAsyncDisposable> Acquire(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        _ = await gate.Reader.ReadAsync(ct).ConfigureAwait(false);
        acquiredSlugs.Add(slug);
        return new Lease(gate.Writer);
    }

    private static Channel<bool> CreateGate()
    {
        var gate = Channel.CreateBounded<bool>(1);
        _ = gate.Writer.TryWrite(true);
        return gate;
    }

    private sealed class Lease(ChannelWriter<bool> writer) : IAsyncDisposable
    {
        private ChannelWriter<bool>? writer = writer;

        public ValueTask DisposeAsync()
        {
            _ = Interlocked.Exchange(ref writer, null)?.TryWrite(true);
            return ValueTask.CompletedTask;
        }
    }
}
