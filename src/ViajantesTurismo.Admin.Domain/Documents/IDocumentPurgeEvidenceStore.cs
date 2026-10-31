namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Persists counts-only evidence of draft and idempotency retention purge runs.</summary>
public interface IDocumentPurgeEvidenceStore
{
    /// <summary>Stages purge evidence for insertion on the next save.</summary>
    void Add(DocumentRetentionPurgeRecord record);

    /// <summary>Discards every change staged and not yet saved, so a failed run cannot leak into later evidence.</summary>
    void DiscardPendingChanges();
}
