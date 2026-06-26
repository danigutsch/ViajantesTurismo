using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public static class InMemoryCatalogTourReadModelStoreTestsHelpers
{
    public static CatalogTourDraftReadModel CreateTour(Guid id, string title)
    {
        return new CatalogTourDraftReadModel(
            id,
            Guid.CreateVersion7(),
            $"TOUR-{title}",
            title,
            1,
            DateTimeOffset.UtcNow);
    }
}
