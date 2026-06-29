using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class EfPublicMediaImageStore(CatalogDbContext dbContext) : IPublicMediaImageStore
{
    public async ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(image);

        var sanitizedImage = Sanitize(image);

        var existing = await dbContext.PublicMediaImages
            .Include(current => current.ResponsiveVariants)
            .Include(current => current.TourLinks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(current => current.Id == sanitizedImage.Id, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            dbContext.PublicMediaImages.Remove(existing);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        dbContext.PublicMediaImages.Add(sanitizedImage);
        SetResponsiveVariantSortOrder(sanitizedImage);

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        var image = await dbContext.PublicMediaImages
            .Include(current => current.ResponsiveVariants)
            .Include(current => current.TourLinks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(current => current.Id == imageId, ct)
            .ConfigureAwait(false);

        return image is null ? null : ToReturnedImage(image, catalogTourId: null);
    }

    public async ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
    {
        var images = await dbContext.PublicMediaImages
            .Include(image => image.ResponsiveVariants)
            .Include(image => image.TourLinks)
            .AsSplitQuery()
            .Where(image => image.TourLinks.Any(link => link.CatalogTourId == catalogTourId))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        return
        [
            .. images
                .OrderByDescending(image => image.TourLinks.Single(link => link.CatalogTourId == catalogTourId).IsCover)
                .ThenBy(image => image.TourLinks.Single(link => link.CatalogTourId == catalogTourId).DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => ForTour(image, catalogTourId))
        ];
    }

    public async ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<PublicMediaImage>>> ListByTours(
        IReadOnlyCollection<Guid> catalogTourIds,
        CancellationToken ct)
    {
        var requestedIds = catalogTourIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (requestedIds.Length == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<PublicMediaImage>>();
        }

        var images = await dbContext.PublicMediaImages
            .Include(image => image.ResponsiveVariants)
            .Include(image => image.TourLinks)
            .AsSplitQuery()
            .Where(image => image.TourLinks.Any(link => requestedIds.Contains(link.CatalogTourId)))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        return requestedIds.ToDictionary(
            tourId => tourId,
            tourId => (IReadOnlyList<PublicMediaImage>)
            [
                .. images
                    .Where(image => image.TourLinks.Any(link => link.CatalogTourId == tourId))
                    .OrderByDescending(image => image.TourLinks.Single(link => link.CatalogTourId == tourId).IsCover)
                    .ThenBy(image => image.TourLinks.Single(link => link.CatalogTourId == tourId).DisplayOrder)
                    .ThenBy(image => image.Id)
                    .Select(image => ForTour(image, tourId))
            ]);
    }

    private static PublicMediaImage Sanitize(PublicMediaImage image)
    {
        return new PublicMediaImage(
            image.Id,
            image.SourceUri,
            image.Checksum,
            image.ContentType,
            image.FileSizeBytes,
            image.Dimensions,
            image.ProcessingStatus,
            image.ResponsiveVariants,
            StringSanitizer.SanitizeCollection(image.Tags),
            [.. image.TourLinks.OrderBy(link => link.DisplayOrder)],
            StringSanitizer.Sanitize(image.AltText) ?? string.Empty,
            StringSanitizer.Sanitize(image.Caption),
            StringSanitizer.Sanitize(image.Attribution),
            StringSanitizer.Sanitize(image.Copyright));
    }

    private PublicMediaImage ForTour(PublicMediaImage image, Guid catalogTourId)
    {
        return ToReturnedImage(image, catalogTourId);
    }

    private PublicMediaImage ToReturnedImage(PublicMediaImage image, Guid? catalogTourId)
    {
        var sanitizedImage = Sanitize(image);

        return new PublicMediaImage(
            sanitizedImage.Id,
            sanitizedImage.SourceUri,
            sanitizedImage.Checksum,
            sanitizedImage.ContentType,
            sanitizedImage.FileSizeBytes,
            sanitizedImage.Dimensions,
            sanitizedImage.ProcessingStatus,
            [.. sanitizedImage.ResponsiveVariants.OrderBy(ResponsiveVariantSortOrder)],
            sanitizedImage.Tags,
            [.. sanitizedImage.TourLinks
                .Where(link => catalogTourId is null || link.CatalogTourId == catalogTourId.Value)
                .OrderBy(link => link.DisplayOrder)],
            sanitizedImage.AltText,
            sanitizedImage.Caption,
            sanitizedImage.Attribution,
            sanitizedImage.Copyright);
    }

    private void SetResponsiveVariantSortOrder(PublicMediaImage image)
    {
        dbContext.ChangeTracker.DetectChanges();

        for (var index = 0; index < image.ResponsiveVariants.Count; index++)
        {
            dbContext.Entry(image.ResponsiveVariants[index]).Property("SortOrder").CurrentValue = index;
        }
    }

    private int ResponsiveVariantSortOrder(MediaImageResponsiveVariant variant)
    {
        return dbContext.Entry(variant).Property<int>("SortOrder").CurrentValue;
    }
}
