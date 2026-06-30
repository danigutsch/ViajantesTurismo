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
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(() => ReplaceImage(sanitizedImage, ct)).ConfigureAwait(false);
    }

    private async Task ReplaceImage(PublicMediaImage image, CancellationToken ct)
    {
        var existing = await dbContext.PublicMediaImages
            .Include(current => current.ResponsiveVariants)
            .Include(current => current.TourLinks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(current => current.Id == image.Id, ct)
            .ConfigureAwait(false);

        if (existing is not null && dbContext.Database.IsRelational())
        {
            var transaction = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            await using var _ = transaction.ConfigureAwait(false);
            dbContext.PublicMediaImages.Remove(existing);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            dbContext.PublicMediaImages.Add(image);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);
        }
        else
        {
            if (existing is not null)
            {
                dbContext.PublicMediaImages.Remove(existing);
                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            dbContext.PublicMediaImages.Add(image);

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
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
            new PublicMediaImageMetadata
            {
                Id = image.Id,
                SourceUri = image.SourceUri,
                Checksum = image.Checksum,
                ContentType = image.ContentType,
                FileSizeBytes = image.FileSizeBytes,
                Dimensions = image.Dimensions,
                ProcessingStatus = image.ProcessingStatus,
                AltText = StringSanitizer.Sanitize(image.AltText) ?? string.Empty,
                Caption = StringSanitizer.Sanitize(image.Caption),
                Attribution = StringSanitizer.Sanitize(image.Attribution),
                Copyright = StringSanitizer.Sanitize(image.Copyright)
            },
            image.ResponsiveVariants,
            StringSanitizer.SanitizeCollection(image.Tags),
            [.. image.TourLinks.OrderBy(link => link.DisplayOrder)]);
    }

    private static PublicMediaImage ForTour(PublicMediaImage image, Guid catalogTourId)
    {
        return ToReturnedImage(image, catalogTourId);
    }

    private static PublicMediaImage ToReturnedImage(PublicMediaImage image, Guid? catalogTourId)
    {
        var sanitizedImage = Sanitize(image);

        return new PublicMediaImage(
            new PublicMediaImageMetadata
            {
                Id = sanitizedImage.Id,
                SourceUri = sanitizedImage.SourceUri,
                Checksum = sanitizedImage.Checksum,
                ContentType = sanitizedImage.ContentType,
                FileSizeBytes = sanitizedImage.FileSizeBytes,
                Dimensions = sanitizedImage.Dimensions,
                ProcessingStatus = sanitizedImage.ProcessingStatus,
                AltText = sanitizedImage.AltText,
                Caption = sanitizedImage.Caption,
                Attribution = sanitizedImage.Attribution,
                Copyright = sanitizedImage.Copyright
            },
            [.. sanitizedImage.ResponsiveVariants.OrderBy(variant => variant.SortOrder)],
            sanitizedImage.Tags,
            [.. sanitizedImage.TourLinks
                .Where(link => catalogTourId is null || link.CatalogTourId == catalogTourId.Value)
                .OrderBy(link => link.DisplayOrder)]);
    }
}
