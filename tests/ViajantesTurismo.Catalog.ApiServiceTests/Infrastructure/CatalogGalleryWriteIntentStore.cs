using ViajantesTurismo.Catalog.Application.Gallery;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Records accepted snapshots for the HTTP editor tests.</summary>
internal sealed class CatalogGalleryWriteIntentStore : ICatalogGalleryPlacementIntentStore
{
    private readonly Lock gate = new();
    private readonly Dictionary<Guid, List<CatalogGalleryPlacementIntent>> journals = [];

    /// <inheritdoc />
    public ValueTask<long> Enqueue(
        Guid catalogTourId, IReadOnlyList<Guid> photoIds, Guid? coverId, bool canPublish, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (!journals.TryGetValue(catalogTourId, out var journal))
            {
                journal = [];
                journals.Add(catalogTourId, journal);
            }

            var sequence = journal.Count + 1L;
            journal.Add(new CatalogGalleryPlacementIntent(
                Guid.CreateVersion7(), sequence, Array.AsReadOnly(photoIds.ToArray()), coverId, canPublish, AppendsUploadedPhoto: false));
            return ValueTask.FromResult(sequence);
        }
    }

    /// <inheritdoc />
    public ValueTask<CatalogGalleryPlacementIntent?> GetIntent(Guid catalogTourId, long sequence, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            var intent = journals.TryGetValue(catalogTourId, out var journal) && sequence > 0 && sequence <= journal.Count
                ? journal[(int)sequence - 1]
                : null;
            return ValueTask.FromResult(intent);
        }
    }

    /// <inheritdoc />
    public ValueTask<long> GetHighWatermark(Guid catalogTourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            return ValueTask.FromResult(journals.TryGetValue(catalogTourId, out var journal) ? journal.Count : 0L);
        }
    }

    /// <inheritdoc />
    public ValueTask ResumeBootstrap(Guid catalogTourId, long throughSequence, Guid bootstrapEventId, CancellationToken ct) =>
        throw new NotSupportedException("HTTP editor fixtures do not bootstrap legacy placement.");
}
