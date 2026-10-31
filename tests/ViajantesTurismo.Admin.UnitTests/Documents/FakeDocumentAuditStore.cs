using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>In-memory document audit store fake.</summary>
internal sealed class FakeDocumentAuditStore : IDocumentAuditStore
{
    /// <summary>Audit records added to the store.</summary>
    public List<DocumentAuditRecord> Records { get; } = [];

    /// <summary>Purge evidence added to the store.</summary>
    public List<DocumentAuditPurgeRecord> PurgeRecords { get; } = [];

    /// <summary>Exception thrown by <c>PurgeExpiredRecords</c> when set.</summary>
    public Exception? PurgeFailure
    {
        get; set;
    }

    /// <summary>Lineages of the expired candidates the fake offers to the held-lineage filter.</summary>
    public List<Guid> CandidateLineageIds { get; } = [];

    /// <summary>Count returned by <c>PurgeExpiredRecords</c>.</summary>
    public int PurgedCount
    {
        get; set;
    }

    /// <summary>Number of times staged changes were discarded.</summary>
    public int DiscardCount
    {
        get; private set;
    }

    /// <summary>Time passed to the last purge, or null when no purge ran.</summary>
    public DateTime? PurgeCalledAt
    {
        get; private set;
    }

    /// <summary>Adds the record to <see cref="Records" />.</summary>
    public void Add(DocumentAuditRecord record) => Records.Add(record);

    /// <summary>Adds the evidence to <see cref="PurgeRecords" />.</summary>
    public void Add(DocumentAuditPurgeRecord record) => PurgeRecords.Add(record);

    /// <summary>Captures the purge time and returns <see cref="PurgedCount" />, or throws <see cref="PurgeFailure" />.</summary>
    public async Task<DocumentPurgeCounts> PurgeExpiredRecords(DateTime now, HeldDocumentLineages heldLineages, CancellationToken ct)
    {
        PurgeCalledAt = now;
        if (PurgeFailure is not null)
        {
            throw PurgeFailure;
        }

        var held = await heldLineages(CandidateLineageIds, ct);
        return new DocumentPurgeCounts(PurgedCount, held.Count);
    }

    /// <summary>Counts the discard and drops any evidence staged so far.</summary>
    public void DiscardPendingChanges()
    {
        DiscardCount++;
        PurgeRecords.Clear();
    }
}
