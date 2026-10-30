namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Removes document idempotency entries that can no longer replay.</summary>
public interface IDocumentIdempotencyRetentionStore
{
    /// <summary>
    /// Removes one batch of document idempotency entries whose completion, or start when never completed,
    /// is at or before <paramref name="cutoff" />, and returns how many were removed.
    /// </summary>
    Task<int> PurgeExpiredEntries(DateTimeOffset cutoff, CancellationToken ct);
}
