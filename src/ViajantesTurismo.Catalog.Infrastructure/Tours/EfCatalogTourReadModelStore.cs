using Microsoft.EntityFrameworkCore;
using SharedKernel.BuildingBlocks;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore.Npgsql;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using SharedKernel.InputNormalization;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>
/// EF Core store for the Catalog tour read model, applying projection events idempotently by stream position.
/// </summary>
/// <param name="dbContext">The Catalog context.</param>
internal sealed class EfCatalogTourReadModelStore(
    CatalogDbContext dbContext) : ICatalogTourReadModelStore
{
    private const int MaxProjectionSaveAttempts = 3;

    private static readonly NpgsqlUniqueViolationParser UniqueViolationParser = new();

    /// <inheritdoc />
    public async ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tour);

        var existing = await dbContext.CatalogTourReadModels
            .SingleOrDefaultAsync(current => current.CatalogTourId == tour.CatalogTourId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            dbContext.CatalogTourReadModels.Add(ToEntity(tour));
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        existing.AdminTourId = tour.AdminTourId;
        existing.Identifier = tour.Identifier;
        existing.Position = Math.Max(existing.Position, tour.Position);
        existing.PresentationPosition = Math.Max(existing.PresentationPosition, tour.Position);
        existing.StreamVersion = Math.Max(existing.StreamVersion, tour.StreamVersion);
        existing.UpdatedAt = existing.UpdatedAt >= tour.UpdatedAt ? existing.UpdatedAt : tour.UpdatedAt;

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(
        Guid catalogTourId,
        CatalogTourPresentationUpdate update,
        long streamVersion,
        long position,
        DateTimeOffset updatedAt,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);

        var normalizedSlug = StringSanitizer.Sanitize(update.Slug) ?? string.Empty;
        for (var attempt = 1; attempt <= MaxProjectionSaveAttempts; attempt++)
        {
            var existing = await dbContext.CatalogTourReadModels
                .SingleOrDefaultAsync(current => current.CatalogTourId == catalogTourId, ct)
                .ConfigureAwait(false);
            if (existing is null)
            {
                return null;
            }

            if (position <= existing.PresentationPosition)
            {
                return ToReadModel(existing);
            }

            existing.Title = StringSanitizer.Sanitize(update.Title) ?? string.Empty;
            existing.Slug = normalizedSlug;
            existing.Summary = StringSanitizer.Sanitize(update.Summary) ?? string.Empty;
            existing.Description = StringSanitizer.Sanitize(update.Description) ?? string.Empty;
            existing.Itinerary = StringSanitizer.Sanitize(update.Itinerary) ?? string.Empty;
            existing.SeoTitle = StringSanitizer.Sanitize(update.SeoTitle) ?? string.Empty;
            existing.SeoDescription = StringSanitizer.Sanitize(update.SeoDescription) ?? string.Empty;
            var domainEditsRequireDraftSoUnpublishPrecededPresentation = position > existing.PublicationPosition;
            if (domainEditsRequireDraftSoUnpublishPrecededPresentation)
            {
                existing.IsPublished = false;
            }

            existing.StreamVersion = Math.Max(existing.StreamVersion, streamVersion);
            existing.PresentationPosition = position;
            existing.Position = Math.Max(existing.Position, position);
            existing.UpdatedAt = existing.UpdatedAt >= updatedAt ? existing.UpdatedAt : updatedAt;

            if (await SaveProjection(normalizedSlug, ct).ConfigureAwait(false))
            {
                return ToReadModel(existing);
            }
        }

        throw new DbUpdateConcurrencyException("Catalog tour presentation projection remained stale after retries.");
    }

    /// <inheritdoc />
    public async ValueTask<CatalogTourDraftReadModel?> SetPublicationStatus(
        Guid catalogTourId,
        bool isPublished,
        long streamVersion,
        long position,
        DateTimeOffset updatedAt,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxProjectionSaveAttempts; attempt++)
        {
            var existing = await dbContext.CatalogTourReadModels
                .SingleOrDefaultAsync(current => current.CatalogTourId == catalogTourId, ct)
                .ConfigureAwait(false);
            if (existing is null)
            {
                return null;
            }

            if (position <= existing.PublicationPosition)
            {
                return ToReadModel(existing);
            }

            var publicationFollowsPresentation = position > existing.PresentationPosition;
            existing.IsPublished = isPublished && publicationFollowsPresentation;
            existing.StreamVersion = Math.Max(existing.StreamVersion, streamVersion);
            existing.PublicationPosition = position;
            existing.Position = Math.Max(existing.Position, position);
            existing.UpdatedAt = existing.UpdatedAt >= updatedAt ? existing.UpdatedAt : updatedAt;

            if (await SaveProjection(slug: null, ct).ConfigureAwait(false))
            {
                return ToReadModel(existing);
            }
        }

        throw new DbUpdateConcurrencyException("Catalog tour publication projection remained stale after retries.");
    }

    /// <inheritdoc />
    public async ValueTask<CatalogTourDraftReadModel?> UpdateGallery(
        CatalogTourGalleryChanged gallery, long streamVersion, long position, DateTimeOffset updatedAt, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(gallery);

        for (var attempt = 1; attempt <= MaxProjectionSaveAttempts; attempt++)
        {
            var existing = await dbContext.CatalogTourReadModels
                .SingleOrDefaultAsync(current => current.CatalogTourId == gallery.CatalogTourId, ct)
                .ConfigureAwait(false);
            if (existing is null)
            {
                return null;
            }

            if (position <= existing.GalleryPosition)
            {
                return ToReadModel(existing);
            }

            existing.GalleryPhotoIds = [.. gallery.PhotoIds];
            existing.GalleryCoverId = gallery.CoverId;
            existing.GalleryPosition = position;
            existing.StreamVersion = Math.Max(existing.StreamVersion, streamVersion);
            existing.Position = Math.Max(existing.Position, position);
            existing.UpdatedAt = existing.UpdatedAt >= updatedAt ? existing.UpdatedAt : updatedAt;

            if (await SaveProjection(slug: null, ct).ConfigureAwait(false))
            {
                return ToReadModel(existing);
            }
        }

        throw new DbUpdateConcurrencyException("Catalog tour gallery projection remained stale after retries.");
    }

    /// <inheritdoc />
    public async ValueTask<bool> AdvanceStreamVersion(Guid catalogTourId, long streamVersion, CancellationToken ct)
    {
        var tour = dbContext.CatalogTourReadModels.Where(current => current.CatalogTourId == catalogTourId);
        var changed = await tour.Where(current => current.StreamVersion < streamVersion)
            .ExecuteUpdateAsync(setters => setters.SetProperty(current => current.StreamVersion, streamVersion), ct)
            .ConfigureAwait(false);
        return changed > 0 || await tour.AnyAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        return await dbContext.CatalogTourReadModels
            .OrderBy(tour => tour.Title)
            .ThenBy(tour => tour.CatalogTourId)
            .Select(tour => ToReadModel(tour, dbContext.CatalogTourRetirements.Any(retirement => retirement.AdminTourId == tour.AdminTourId)))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<CatalogTourDraftReadModel?> GetTour(Guid catalogTourId, CancellationToken ct)
    {
        return await dbContext.CatalogTourReadModels
            .AsNoTracking()
            .Where(tour => tour.CatalogTourId == catalogTourId)
            .Select(tour => ToReadModel(tour, dbContext.CatalogTourRetirements.Any(retirement => retirement.AdminTourId == tour.AdminTourId)))
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        if (!CatalogTourSlug.TryNormalize(slug, out var normalizedSlug))
        {
            return null;
        }

        var tour = await VisibleTours(dbContext)
            .SingleOrDefaultAsync(tour => tour.Slug == normalizedSlug, ct)
            .ConfigureAwait(false);

        return tour is null ? null : ToReadModel(tour);
    }

    /// <summary>Queries tours with a current public publication and no durable retirement.</summary>
    internal static IQueryable<CatalogTourReadModelEntity> VisibleTours(CatalogDbContext context) =>
        context.CatalogTourReadModels.AsNoTracking().Where(tour => tour.IsPublished
            && !context.CatalogTourRetirements.Any(retirement => retirement.AdminTourId == tour.AdminTourId));

    private static CatalogTourReadModelEntity ToEntity(CatalogTourDraftReadModel tour)
    {
        return new CatalogTourReadModelEntity
        {
            CatalogTourId = tour.CatalogTourId,
            AdminTourId = tour.AdminTourId,
            Identifier = tour.Identifier,
            Title = StringSanitizer.Sanitize(tour.Title) ?? string.Empty,
            Slug = StringSanitizer.Sanitize(tour.Slug) ?? string.Empty,
            Summary = StringSanitizer.Sanitize(tour.Summary) ?? string.Empty,
            Description = StringSanitizer.Sanitize(tour.Description) ?? string.Empty,
            Itinerary = StringSanitizer.Sanitize(tour.Itinerary) ?? string.Empty,
            SeoTitle = StringSanitizer.Sanitize(tour.SeoTitle) ?? string.Empty,
            SeoDescription = StringSanitizer.Sanitize(tour.SeoDescription) ?? string.Empty,
            IsPublished = tour.IsPublished,
            GalleryPosition = tour.GalleryPosition,
            GalleryPhotoIds = tour.GalleryPhotoIds is { } photoIds ? [.. photoIds] : null,
            GalleryCoverId = tour.GalleryCoverId,
            StreamVersion = tour.StreamVersion,
            PresentationPosition = tour.Position,
            PublicationPosition = tour.IsPublished ? tour.Position : 0,
            Position = tour.Position,
            UpdatedAt = tour.UpdatedAt
        };
    }

    private static CatalogTourDraftReadModel ToReadModel(CatalogTourReadModelEntity tour, bool isRetired = false)
    {
        return new CatalogTourDraftReadModel(
            tour.CatalogTourId,
            tour.AdminTourId,
            tour.Identifier,
            tour.Title,
            tour.Slug,
            tour.IsPublished && !isRetired,
            tour.Position,
            tour.UpdatedAt)
        {
            Summary = tour.Summary,
            Description = tour.Description,
            Itinerary = tour.Itinerary,
            SeoTitle = tour.SeoTitle,
            SeoDescription = tour.SeoDescription,
            StreamVersion = tour.StreamVersion,
            GalleryPosition = tour.GalleryPosition,
            GalleryPhotoIds = tour.GalleryPhotoIds is { } photoIds ? Array.AsReadOnly([.. photoIds]) : null,
            GalleryCoverId = tour.GalleryCoverId
        };
    }

    private async ValueTask<bool> SaveProjection(string? slug, CancellationToken ct)
    {
        try
        {
            await dbContext.SaveChangesTranslatingUniqueViolations(UniqueViolationParser, ct).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
        catch (UniqueConstraintViolationException exception) when (
            slug is not null && exception.Rule == CatalogTourReadModelEntityConfiguration.SlugUniqueRule)
        {
            throw new CatalogTourSlugConflictException(
                $"The public tour slug '{slug}' is already in use.",
                exception);
        }
    }
}
