namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges document idempotency entries once their replay window has elapsed.</summary>
public sealed class PurgeExpiredDocumentIdempotencyEntriesCommandHandler(
    IDocumentIdempotencyRetentionStore retentionStore,
    TimeProvider timeProvider)
{
    /// <summary>Removes expired document idempotency entries and returns the count.</summary>
    public async Task<int> Handle(CancellationToken ct)
    {
        return await retentionStore.PurgeExpiredEntries(
            timeProvider.GetUtcNow() - DocumentCommandIdempotency.ReplayWindow, ct);
    }
}
