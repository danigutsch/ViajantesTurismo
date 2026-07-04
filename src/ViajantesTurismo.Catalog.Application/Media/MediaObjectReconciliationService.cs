namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Reconciles media metadata references with object storage inventory.
/// </summary>
public sealed class MediaObjectReconciliationService(
    IMediaObjectStore objectStore,
    IPublicMediaImageStore imageStore)
{
    private const string DefaultPrefix = "media/";

    /// <summary>
    /// Builds a reconciliation report and optionally deletes confirmed orphan objects.
    /// </summary>
    /// <param name="deleteOrphans">Whether to delete stored objects that have no metadata reference.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The reconciliation report.</returns>
    public async ValueTask<MediaObjectReconciliationReport> Reconcile(bool deleteOrphans, CancellationToken ct)
    {
        var referencedKeys = await imageStore.ListReferencedObjectKeys(ct).ConfigureAwait(false);
        var referencedKeySet = referencedKeys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.Ordinal);
        var storedKeySet = (await objectStore.ListKeys(DefaultPrefix, ct).ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);

        var missing = new List<string>();
        foreach (var referencedKey in referencedKeySet.Order(StringComparer.Ordinal))
        {
            if (!await objectStore.Exists(referencedKey, ct).ConfigureAwait(false))
            {
                missing.Add(referencedKey);
            }
        }

        var orphans = storedKeySet.Except(referencedKeySet).Order(StringComparer.Ordinal).ToArray();
        var deleted = new List<string>();
        if (deleteOrphans)
        {
            foreach (var orphan in orphans)
            {
                if (await objectStore.Exists(orphan, ct).ConfigureAwait(false))
                {
                    await objectStore.Delete(orphan, ct).ConfigureAwait(false);
                    deleted.Add(orphan);
                }
            }
        }

        return new MediaObjectReconciliationReport(missing, orphans, deleted);
    }
}
