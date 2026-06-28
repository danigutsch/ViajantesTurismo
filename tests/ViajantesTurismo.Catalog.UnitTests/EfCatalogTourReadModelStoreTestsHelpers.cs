using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class EfCatalogTourReadModelStoreTestsHelpers
{
    public static CatalogTourDraftReadModel CreateTour(Guid id, string title)
    {
        return new CatalogTourDraftReadModel(
            id,
            Guid.CreateVersion7(),
            $"TOUR-{title}",
            title,
            title,
            false,
            1,
            DateTimeOffset.UtcNow);
    }
}
