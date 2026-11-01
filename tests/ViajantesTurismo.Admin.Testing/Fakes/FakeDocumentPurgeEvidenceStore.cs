using System.Collections.ObjectModel;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>In-memory purge evidence store fake.</summary>
public sealed class FakeDocumentPurgeEvidenceStore : IDocumentPurgeEvidenceStore
{
    /// <summary>Gets the evidence added to the store.</summary>
    public Collection<DocumentRetentionPurgeRecord> Records { get; } = [];

    /// <summary>Gets how many times staged changes were discarded.</summary>
    public int DiscardCount
    {
        get; private set;
    }

    /// <summary>Adds the evidence to <see cref="Records" />.</summary>
    public void Add(DocumentRetentionPurgeRecord record) => Records.Add(record);

    /// <summary>Counts the discard and drops any evidence staged so far.</summary>
    public void DiscardPendingChanges()
    {
        DiscardCount++;
        Records.Clear();
    }
}
