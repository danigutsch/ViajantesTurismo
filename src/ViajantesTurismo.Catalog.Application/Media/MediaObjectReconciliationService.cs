using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Reconciles media metadata references with object storage inventory.
/// </summary>
public sealed class MediaObjectReconciliationService(
    IMediaObjectStore objectStore,
    IPublicMediaImageStore imageStore,
    TimeProvider? timeProvider = null,
    ILogger<MediaObjectReconciliationService>? logger = null)
{
    private const string DefaultPrefix = "media/";

    private static readonly Action<ILogger, string, Exception?> DeleteFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1, nameof(LogDeleteFailed)),
        "Failed to delete orphan media object {ObjectKey} during reconciliation.");

    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Builds a reconciliation report and optionally deletes confirmed orphan objects.
    /// </summary>
    /// <param name="deleteOrphans">Whether to delete stored objects that have no metadata reference.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The reconciliation report.</returns>
    public async ValueTask<MediaObjectReconciliationReport> Reconcile(bool deleteOrphans, CancellationToken ct)
    {
        return await Reconcile(deleteOrphans, TimeSpan.Zero, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a reconciliation report and optionally deletes confirmed orphan objects older than a grace period.
    /// </summary>
    /// <param name="deleteOrphans">Whether to delete stored objects that have no metadata reference.</param>
    /// <param name="orphanGracePeriod">The minimum orphan age before deletion.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The reconciliation report.</returns>
    public async ValueTask<MediaObjectReconciliationReport> Reconcile(bool deleteOrphans, TimeSpan orphanGracePeriod, CancellationToken ct)
    {
        if (orphanGracePeriod < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(orphanGracePeriod), orphanGracePeriod, "Orphan grace period cannot be negative.");
        }

        var referencedKeys = await imageStore.ListReferencedObjectKeys(ct).ConfigureAwait(false);
        var referencedKeySet = referencedKeys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.Ordinal);
        var storedObjects = await objectStore.ListObjects(DefaultPrefix, ct).ConfigureAwait(false);
        var storedByKey = storedObjects
            .Where(static item => !string.IsNullOrWhiteSpace(item.ObjectKey))
            .GroupBy(static item => item.ObjectKey, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        var storedKeySet = storedByKey.Keys.ToHashSet(StringComparer.Ordinal);

        var missing = referencedKeySet
            .Where(referencedKey => !storedKeySet.Contains(referencedKey))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var orphans = storedKeySet.Except(referencedKeySet).Order(StringComparer.Ordinal).ToArray();
        var deleted = new List<string>();
        var failed = new List<string>();
        if (deleteOrphans)
        {
            var deleteBefore = timeProvider.GetUtcNow() - orphanGracePeriod;
            foreach (var orphan in orphans.Where(orphan => storedByKey[orphan].LastModifiedAt <= deleteBefore))
            {
                try
                {
                    await objectStore.Delete(orphan, ct).ConfigureAwait(false);
                    deleted.Add(orphan);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
                {
                    RecordDeleteFailure(orphan, failed, exception);
                }
            }
        }

        CatalogTelemetry.MediaObjectReconciliationObjects.Add(orphans.Length, CreateTags(CatalogTelemetry.OutcomeSuccess, "orphan"));
        CatalogTelemetry.MediaObjectReconciliationObjects.Add(deleted.Count, CreateTags(CatalogTelemetry.OutcomeSuccess, "delete"));
        CatalogTelemetry.MediaObjectReconciliationObjects.Add(missing.Length, CreateTags(CatalogTelemetry.OutcomeSuccess, "missing"));

        return new MediaObjectReconciliationReport(missing, orphans, deleted, failed);
    }

    private static KeyValuePair<string, object?>[] CreateTags(string outcome, string operation) =>
    [
        new(CatalogTelemetry.TagOutcome, outcome),
        new(CatalogTelemetry.TagMediaObjectReconciliationOperation, operation)
    ];

    private void RecordDeleteFailure(string orphan, List<string> failed, Exception exception)
    {
        failed.Add(orphan);
        LogDeleteFailed(logger, exception, orphan);
        CatalogTelemetry.MediaObjectReconciliationObjects.Add(1, CreateTags(CatalogTelemetry.OutcomeError, "delete"));
    }

    private static void LogDeleteFailed(ILogger? logger, Exception exception, string objectKey)
    {
        if (logger is not null)
        {
            DeleteFailed(logger, objectKey, exception);
        }
    }
}
