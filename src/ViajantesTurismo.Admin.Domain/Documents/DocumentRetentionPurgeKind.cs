namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Names which retention purge a run of evidence belongs to.</summary>
public enum DocumentRetentionPurgeKind
{
    /// <summary>The purge of expired unfinalized document drafts.</summary>
    Drafts,

    /// <summary>The purge of expired document idempotency entries.</summary>
    IdempotencyEntries
}
