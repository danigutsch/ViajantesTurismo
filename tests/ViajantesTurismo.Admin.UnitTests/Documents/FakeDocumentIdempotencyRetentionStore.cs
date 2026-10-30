using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>In-memory document idempotency retention store fake.</summary>
internal sealed class FakeDocumentIdempotencyRetentionStore : IDocumentIdempotencyRetentionStore
{
    /// <summary>Count returned by <c>PurgeExpiredEntries</c>.</summary>
    public int PurgedCount
    {
        get; set;
    }

    /// <summary>Cutoff passed to the last purge, or null when no purge ran.</summary>
    public DateTimeOffset? PurgeCutoff
    {
        get; private set;
    }

    /// <summary>Captures the cutoff and returns <see cref="PurgedCount" />.</summary>
    public Task<int> PurgeExpiredEntries(DateTimeOffset cutoff, CancellationToken ct)
    {
        PurgeCutoff = cutoff;
        return Task.FromResult(PurgedCount);
    }
}
