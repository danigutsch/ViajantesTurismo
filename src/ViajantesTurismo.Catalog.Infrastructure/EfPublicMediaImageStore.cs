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

        var existing = await dbContext.PublicMediaImages
            .Include(current => current.ResponsiveVariants)
            .Include(current => current.TourLinks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(current => current.Id == image.Id, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            CopyToEntity(image, existing);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        dbContext.PublicMediaImages.Add(ToEntity(image));
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

        return image is null ? null : ToDomain(image);
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
                .Select(image => ToDomain(image, catalogTourId))
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
                    .Select(image => ToDomain(image, tourId))
            ]);
    }

    private static PublicMediaImageEntity ToEntity(PublicMediaImage image)
    {
        var entity = new PublicMediaImageEntity
        {
            Id = image.Id
        };

        CopyScalarValues(image, entity);
        CopyChildren(image, entity);

        return entity;
    }

    private static void CopyToEntity(PublicMediaImage image, PublicMediaImageEntity entity)
    {
        CopyScalarValues(image, entity);
        entity.ResponsiveVariants.Clear();
        entity.TourLinks.Clear();
        CopyChildren(image, entity);
    }

    private static void CopyScalarValues(PublicMediaImage image, PublicMediaImageEntity entity)
    {
        entity.SourceUri = image.SourceUri.ToString();
        entity.Checksum = image.Checksum;
        entity.ContentType = image.ContentType;
        entity.FileSizeBytes = image.FileSizeBytes;
        entity.Width = image.Dimensions.Width;
        entity.Height = image.Dimensions.Height;
        entity.ProcessingStatus = image.ProcessingStatus;
        entity.Tags = [.. image.Tags.Select(tag => StringSanitizer.Sanitize(tag) ?? string.Empty)];
        entity.AltText = StringSanitizer.Sanitize(image.AltText) ?? string.Empty;
        entity.Caption = StringSanitizer.Sanitize(image.Caption);
        entity.Attribution = StringSanitizer.Sanitize(image.Attribution);
        entity.Copyright = StringSanitizer.Sanitize(image.Copyright);
    }

    private static void CopyChildren(PublicMediaImage image, PublicMediaImageEntity entity)
    {
        CopyResponsiveVariants(image, entity);
        CopyTourLinks(image, entity);
    }

    private static void CopyResponsiveVariants(PublicMediaImage image, PublicMediaImageEntity entity)
    {
        for (var index = 0; index < image.ResponsiveVariants.Count; index++)
        {
            var variant = image.ResponsiveVariants[index];
            entity.ResponsiveVariants.Add(new PublicMediaImageResponsiveVariantEntity
            {
                PublicMediaImageId = image.Id,
                SortOrder = index,
                Uri = variant.Uri.ToString(),
                Width = variant.Width,
                Height = variant.Height,
                ContentType = variant.ContentType,
                FileSizeBytes = variant.FileSizeBytes
            });
        }
    }

    private static void CopyTourLinks(PublicMediaImage image, PublicMediaImageEntity entity)
    {
        foreach (var link in image.TourLinks)
        {
            entity.TourLinks.Add(new PublicMediaImageTourLinkEntity
            {
                PublicMediaImageId = image.Id,
                CatalogTourId = link.CatalogTourId,
                DisplayOrder = link.DisplayOrder,
                IsCover = link.IsCover
            });
        }
    }

    private static PublicMediaImage ToDomain(PublicMediaImageEntity image)
    {
        return ToDomain(image, catalogTourId: null);
    }

    private static PublicMediaImage ToDomain(PublicMediaImageEntity image, Guid? catalogTourId)
    {
        return new PublicMediaImage(
            image.Id,
            new Uri(image.SourceUri, UriKind.RelativeOrAbsolute),
            image.Checksum,
            image.ContentType,
            image.FileSizeBytes,
            new MediaImageDimensions(image.Width, image.Height),
            image.ProcessingStatus,
            [.. image.ResponsiveVariants
                .OrderBy(variant => variant.SortOrder)
                .Select(variant => new MediaImageResponsiveVariant(
                    new Uri(variant.Uri, UriKind.RelativeOrAbsolute),
                    variant.Width,
                    variant.Height,
                    variant.ContentType,
                    variant.FileSizeBytes))],
            image.Tags,
            [.. image.TourLinks
                .Where(link => catalogTourId is null || link.CatalogTourId == catalogTourId.Value)
                .OrderBy(link => link.DisplayOrder)
                .Select(link => new MediaImageTourLink(link.CatalogTourId, link.DisplayOrder, link.IsCover))],
            image.AltText,
            image.Caption,
            image.Attribution,
            image.Copyright);
    }
}
