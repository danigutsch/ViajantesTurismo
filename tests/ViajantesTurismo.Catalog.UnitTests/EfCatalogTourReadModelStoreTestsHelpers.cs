using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Helpers for EF Catalog tour read model store tests.</summary>
internal static class EfCatalogTourReadModelStoreTestsHelpers
{
    /// <summary>Creates an unpublished draft read model with the title also used as slug.</summary>
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
            TimeProvider.System.GetUtcNow());
    }

    /// <summary>Creates a draft read model with the given slug and published state.</summary>
    public static CatalogTourDraftReadModel CreateTour(Guid id, string title, string slug, bool isPublished)
    {
        return new CatalogTourDraftReadModel(
            id,
            Guid.CreateVersion7(),
            $"TOUR-{title}",
            title,
            slug,
            isPublished,
            1,
            TimeProvider.System.GetUtcNow());
    }
}
