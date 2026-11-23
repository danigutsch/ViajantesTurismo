using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using PandaBear.BuildingBlocks;
using PandaBear.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore.Npgsql;
using PandaBear.Npgsql;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using PandaBear.InputNormalization;

namespace ViajantesTurismo.Catalog.Infrastructure.Media;

/// <summary>
/// EF Core store for public media images, sanitizing text fields on write and read.
/// </summary>
/// <param name="dbContext">The Catalog context.</param>
internal sealed class EfPublicMediaImageStore(CatalogDbContext dbContext) : IPublicMediaImageStore, IMediaImageUploadStore
{
    private static readonly NpgsqlUniqueViolationParser UniqueViolationParser = new();

    /// <inheritdoc />
    public async ValueTask<IMediaImageUploadLease> Reserve(string fingerprint, CancellationToken ct)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
            var npgsqlTransaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            var key = PostgreSqlAdvisoryLockKey.FromUtf8Sha256BigEndian("catalog-image-upload:" + fingerprint);
            await PostgreSqlTransactionAdvisoryLock.Acquire(connection, npgsqlTransaction, key, ct).ConfigureAwait(false);

            var existing = await ReadImages()
                .AsSingleQuery()
                .SingleOrDefaultAsync(image => EF.Property<string?>(image, PublicMediaImageSchema.UploadFingerprint) == fingerprint, ct)
                .ConfigureAwait(false);
            return new UploadLease(this, transaction, fingerprint, existing is null ? null : ToReturnedImage(existing, catalogTourId: null));
        }
        catch
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask Upsert(PublicMediaImage image, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(image);

        var sanitizedImage = Sanitize(image);
        var strategy = dbContext.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(() => ReplaceImage(sanitizedImage, ct)).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException exception) when (exception.Rule == PublicMediaImageSchema.GalleryPlacementRule)
        {
            throw new MediaGalleryPlacementConflictException(
                "The tour gallery changed while the media image was being saved.",
                exception);
        }
    }

    private async Task ReplaceImage(PublicMediaImage image, CancellationToken ct)
    {
        var existing = await dbContext.PublicMediaImages
            .Include(current => current.ResponsiveVariants)
            .Include(current => current.TourLinks)
            .Include(current => current.AccessibilityTexts)
            .AsSplitQuery()
            .SingleOrDefaultAsync(current => current.Id == image.Id, ct)
            .ConfigureAwait(false);
        var fingerprint = existing is not null && MatchesUploadOrigin(existing, image)
            ? dbContext.Entry(existing).Property<string?>(PublicMediaImageSchema.UploadFingerprint).CurrentValue
            : null;

        if (existing is not null && dbContext.Database.IsRelational())
        {
            var transaction = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            await using var _ = transaction.ConfigureAwait(false);
            dbContext.PublicMediaImages.Remove(existing);
            await dbContext.SaveChangesTranslatingUniqueViolations(UniqueViolationParser, ct).ConfigureAwait(false);
            AddImage(image, fingerprint);
            await dbContext.SaveChangesTranslatingUniqueViolations(UniqueViolationParser, ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);
        }
        else
        {
            if (existing is not null)
            {
                dbContext.PublicMediaImages.Remove(existing);
                await dbContext.SaveChangesTranslatingUniqueViolations(UniqueViolationParser, ct).ConfigureAwait(false);
            }

            AddImage(image, fingerprint);
            await dbContext.SaveChangesTranslatingUniqueViolations(UniqueViolationParser, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask<PublicMediaImage?> GetImage(Guid imageId, CancellationToken ct)
    {
        var image = await ReadImages()
            .AsSplitQuery()
            .SingleOrDefaultAsync(current => current.Id == imageId, ct)
            .ConfigureAwait(false);

        return image is null ? null : ToReturnedImage(image, catalogTourId: null);
    }

    private IQueryable<PublicMediaImage> ReadImages() =>
        dbContext.PublicMediaImages
            .AsNoTracking()
            .Include(current => current.ResponsiveVariants)
            .Include(current => current.TourLinks)
            .Include(current => current.AccessibilityTexts);

    private void AddImage(PublicMediaImage image, string? fingerprint)
    {
        dbContext.PublicMediaImages.Add(image).Property<string?>(PublicMediaImageSchema.UploadFingerprint).CurrentValue = fingerprint;
    }

    private static bool MatchesUploadOrigin(PublicMediaImage current, PublicMediaImage updated) =>
        current.SourceObjectKey == updated.SourceObjectKey &&
        current.Checksum == updated.Checksum &&
        current.ContentType == updated.ContentType &&
        current.FileSizeBytes == updated.FileSizeBytes &&
        current.Dimensions == updated.Dimensions &&
        current.AltText == updated.AltText &&
        current.Caption == updated.Caption &&
        current.Attribution == updated.Attribution &&
        current.Copyright == updated.Copyright &&
        current.IsDecorative == updated.IsDecorative &&
        current.RequiresHumanReview == updated.RequiresHumanReview &&
        current.IsAiGenerated == updated.IsAiGenerated &&
        current.Tags.SequenceEqual(updated.Tags, StringComparer.Ordinal) &&
        current.TourLinks.Select(link => link.CatalogTourId).Order()
            .SequenceEqual(updated.TourLinks.Select(link => link.CatalogTourId).Order());

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<PublicMediaImage>> ListByTour(Guid catalogTourId, CancellationToken ct)
    {
        var images = await dbContext.PublicMediaImages
            .Include(image => image.ResponsiveVariants)
            .Include(image => image.TourLinks)
            .Include(image => image.AccessibilityTexts)
            .AsSplitQuery()
            .Where(image => image.TourLinks.Any(link => link.CatalogTourId == catalogTourId))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        return
        [
            .. images
                .OrderByDescending(image => image.IsCoverForTour(catalogTourId))
                .ThenBy(image => image.GetDisplayOrderForTour(catalogTourId))
                .ThenBy(image => image.Id)
                .Select(image => ForTour(image, catalogTourId))
        ];
    }

    /// <inheritdoc />
    public async ValueTask<PublicMediaImage?> GetNextPendingReviewByTour(Guid catalogTourId, CancellationToken ct)
    {
        var image = await dbContext.PublicMediaImages
            .AsNoTracking()
            .Where(image => image.TourLinks.Any(link => link.CatalogTourId == catalogTourId))
            .Where(image => image.RequiresHumanReview || image.AccessibilityTexts.Any(text => text.RequiresHumanReview))
            .OrderByDescending(image => image.TourLinks.Any(link => link.CatalogTourId == catalogTourId && link.IsCover))
            .ThenBy(image => image.TourLinks.Where(link => link.CatalogTourId == catalogTourId)
                .Select(link => link.DisplayOrder).FirstOrDefault())
            .ThenBy(image => image.Id)
            .Include(image => image.ResponsiveVariants)
            .Include(image => image.TourLinks.Where(link => link.CatalogTourId == catalogTourId))
            .Include(image => image.AccessibilityTexts)
            .AsSingleQuery()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return image is null ? null : ForTour(image, catalogTourId);
    }

    /// <inheritdoc />
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
            .Include(image => image.AccessibilityTexts)
            .AsSplitQuery()
            .Where(image => image.TourLinks.Any(link => requestedIds.Contains(link.CatalogTourId)))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        return requestedIds.ToDictionary(
            tourId => tourId,
            tourId => (IReadOnlyList<PublicMediaImage>)
            [
                .. images
                    .Where(image => image.BelongsToTour(tourId))
                    .OrderByDescending(image => image.IsCoverForTour(tourId))
                    .ThenBy(image => image.GetDisplayOrderForTour(tourId))
                    .ThenBy(image => image.Id)
                    .Select(image => ForTour(image, tourId))
            ]);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<string>> ListReferencedObjectKeys(CancellationToken ct)
    {
        var sourceKeys = await dbContext.PublicMediaImages
            .AsNoTracking()
            .Select(image => image.SourceObjectKey)
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        var variantKeys = await dbContext.PublicMediaImages
            .AsNoTracking()
            .SelectMany(image => image.ResponsiveVariants.Select(variant => variant.ObjectKey))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        return
        [
            .. sourceKeys
                .Concat(variantKeys)
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
        ];
    }

    private static PublicMediaImage Sanitize(PublicMediaImage image)
    {
        return new PublicMediaImage(
            new PublicMediaImageMetadata
            {
                Id = image.Id,
                SourceObjectKey = image.SourceObjectKey,
                Checksum = image.Checksum,
                ContentType = image.ContentType,
                FileSizeBytes = image.FileSizeBytes,
                Dimensions = image.Dimensions,
                ProcessingStatus = image.ProcessingStatus,
                AltText = StringSanitizer.Sanitize(image.AltText) ?? string.Empty,
                Caption = StringSanitizer.Sanitize(image.Caption),
                IsDecorative = image.IsDecorative,
                RequiresHumanReview = image.RequiresHumanReview,
                IsAiGenerated = image.IsAiGenerated,
                Attribution = StringSanitizer.Sanitize(image.Attribution),
                Copyright = StringSanitizer.Sanitize(image.Copyright)
            },
            image.ResponsiveVariants,
            StringSanitizer.SanitizeCollection(image.Tags),
            [.. image.TourLinks.OrderBy(link => link.DisplayOrder)],
            image.AccessibilityTexts);
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
                SourceObjectKey = sanitizedImage.SourceObjectKey,
                Checksum = sanitizedImage.Checksum,
                ContentType = sanitizedImage.ContentType,
                FileSizeBytes = sanitizedImage.FileSizeBytes,
                Dimensions = sanitizedImage.Dimensions,
                ProcessingStatus = sanitizedImage.ProcessingStatus,
                AltText = sanitizedImage.AltText,
                Caption = sanitizedImage.Caption,
                IsDecorative = sanitizedImage.IsDecorative,
                RequiresHumanReview = sanitizedImage.RequiresHumanReview,
                IsAiGenerated = sanitizedImage.IsAiGenerated,
                Attribution = sanitizedImage.Attribution,
                Copyright = sanitizedImage.Copyright
            },
            [.. sanitizedImage.ResponsiveVariants.OrderBy(variant => variant.SortOrder)],
            sanitizedImage.Tags,
            [.. sanitizedImage.TourLinks
                .Where(link => catalogTourId is null || link.CatalogTourId == catalogTourId.Value)
                .OrderBy(link => link.DisplayOrder)],
            sanitizedImage.AccessibilityTexts);
    }

    private sealed class UploadLease(
        EfPublicMediaImageStore owner,
        IDbContextTransaction transaction,
        string fingerprint,
        PublicMediaImage? existingImage) : IMediaImageUploadLease
    {
        public PublicMediaImage? ExistingImage => existingImage;

        public ValueTask DisposeAsync() => transaction.DisposeAsync();

        public async ValueTask Save(PublicMediaImage image, CancellationToken ct)
        {
            owner.AddImage(Sanitize(image), fingerprint);
            try
            {
                await owner.SaveUploadChanges(ct).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
            catch (UniqueConstraintViolationException exception) when (exception.Rule == PublicMediaImageSchema.GalleryPlacementRule)
            {
                throw new MediaGalleryPlacementConflictException(
                    "The tour gallery changed while the media image was being saved.",
                    exception);
            }
        }
    }

    private Task<int> SaveUploadChanges(CancellationToken ct) =>
        dbContext.SaveChangesTranslatingUniqueViolations(UniqueViolationParser, ct);
}
