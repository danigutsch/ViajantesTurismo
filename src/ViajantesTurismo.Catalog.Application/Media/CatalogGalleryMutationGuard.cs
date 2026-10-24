using PandaBear.EventSourcing;
using PandaBear.Results;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>Coordinates gallery authorization with concurrent publication changes.</summary>
public sealed class CatalogGalleryMutationGuard(
    ICatalogTourMutationLock mutationLock,
    ICatalogTourReadModelStore tourStore,
    IEventStore eventStore)
{
    /// <summary>Acquires a gallery edit lease when every linked tour permits the caller's edit.</summary>
    /// <param name="catalogTourIds">All tours linked to the affected image.</param>
    /// <param name="canPublish">Whether the caller has the Catalog publication permission.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The held mutation lease, or a forbidden result.</returns>
    /// <exception cref="ArgumentNullException">The tour identifiers are null.</exception>
    public async ValueTask<Result<IAsyncDisposable>> Acquire(IReadOnlyCollection<Guid> catalogTourIds, bool canPublish, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(catalogTourIds);
        var lease = await mutationLock.Acquire(catalogTourIds, ct).ConfigureAwait(false);
        try
        {
            if (!canPublish)
            {
                foreach (var id in catalogTourIds.Distinct())
                {
                    var tour = await tourStore.GetTour(id, ct).ConfigureAwait(false);
                    if (tour is null)
                    {
                        await lease.DisposeAsync().ConfigureAwait(false);
                        return Result.Forbidden<IAsyncDisposable>("Gallery changes require an existing draft tour or publication permission.");
                    }

                    var events = await eventStore.Load(CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId), afterRevision: null, ct).ConfigureAwait(false);
                    var aggregate = CatalogTour.Rehydrate(events.OrderBy(envelope => envelope.Revision.Value).Select(envelope => envelope.Data));
                    if (aggregate.Id != id || !aggregate.CanEditGallery(canPublish, tour.IsPubliclyVisible, tour.StreamVersion))
                    {
                        await lease.DisposeAsync().ConfigureAwait(false);
                        return Result.Forbidden<IAsyncDisposable>("Published gallery changes require publication permission.");
                    }
                }
            }

            return Result.Ok(lease);
        }
        catch
        {
            await lease.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
