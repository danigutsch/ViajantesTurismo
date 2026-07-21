using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

internal static class CatalogTourSlugAvailability
{
    private const int BatchSize = 512;

    public static async ValueTask<bool> IsAvailable(
        IEventStore eventStore,
        Guid catalogTourId,
        string slug,
        CancellationToken ct)
    {
        var currentSlugs = new Dictionary<Guid, string>();
        var afterPosition = 0L;

        while (true)
        {
            var batch = await eventStore.LoadAfter(afterPosition, BatchSize, ct).ConfigureAwait(false);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var envelope in batch.OrderBy(envelope => envelope.Position))
            {
                switch (envelope.Data)
                {
                    case CatalogTourDraftCreated created:
                        currentSlugs[created.CatalogTourId] = CatalogTourSlug.RequireCanonical(created.InitialSlug);
                        break;
                    case CatalogTourPresentationChanged changed:
                        currentSlugs[changed.CatalogTourId] = changed.Slug;
                        break;
                }

                afterPosition = envelope.Position;
            }

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        return currentSlugs.All(entry => entry.Key == catalogTourId
            || !string.Equals(entry.Value, slug, StringComparison.Ordinal));
    }
}
