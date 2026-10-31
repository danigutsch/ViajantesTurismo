using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>In-memory document idempotency retention store fake.</summary>
internal sealed class FakeDocumentIdempotencyRetentionStore : IDocumentIdempotencyRetentionStore
{
    /// <summary>Count returned by <c>PurgeExpiredEntries</c>.</summary>
    public int PurgedCount
    {
        get; set;
    }

    /// <summary>Lineages of the expired candidates the fake offers to the held-lineage filter.</summary>
    public List<Guid> CandidateLineageIds { get; } = [];

    /// <summary>Cutoff passed to the last purge, or null when no purge ran.</summary>
    public DateTimeOffset? PurgeCutoff
    {
        get; private set;
    }

    /// <summary>Captures the cutoff and returns <see cref="PurgedCount" />.</summary>
    public async Task<DocumentPurgeCounts> PurgeExpiredEntries(DateTimeOffset cutoff, HeldDocumentLineages heldLineages, CancellationToken ct)
    {
        PurgeCutoff = cutoff;
        var held = await heldLineages(CandidateLineageIds, ct);
        return new DocumentPurgeCounts(PurgedCount, held.Count);
    }
}
