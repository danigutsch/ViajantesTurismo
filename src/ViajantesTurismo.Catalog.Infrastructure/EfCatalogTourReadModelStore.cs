using Microsoft.EntityFrameworkCore;
using Npgsql;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using SharedKernel.InputNormalization;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class EfCatalogTourReadModelStore(
    CatalogDbContext dbContext) : ICatalogTourReadModelStore
{
    private const int MaxProjectionSaveAttempts = 3;

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
            // Domain edits require a draft, so a newer presentation implies an unpublish occurred first.
            var presentationFollowsPublication = position > existing.PublicationPosition;
            if (presentationFollowsPublication)
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

    public async ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        return await dbContext.CatalogTourReadModels
            .OrderBy(tour => tour.Title)
            .ThenBy(tour => tour.CatalogTourId)
            .Select(tour => ToReadModel(tour))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);
    }

    public async ValueTask<CatalogTourDraftReadModel?> GetTour(Guid catalogTourId, CancellationToken ct)
    {
        var tour = await dbContext.CatalogTourReadModels
            .SingleOrDefaultAsync(tour => tour.CatalogTourId == catalogTourId, ct)
            .ConfigureAwait(false);

        return tour is null ? null : ToReadModel(tour);
    }

    public async ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        if (!CatalogTourSlug.TryNormalize(slug, out var normalizedSlug))
        {
            return null;
        }

        var tour = await dbContext.CatalogTourReadModels
            .Where(tour => tour.IsPublished && tour.StreamVersion > 1 && tour.Summary != string.Empty)
            .SingleOrDefaultAsync(tour => tour.Slug == normalizedSlug, ct)
            .ConfigureAwait(false);

        return tour is null ? null : ToReadModel(tour);
    }

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
            StreamVersion = tour.StreamVersion,
            PresentationPosition = tour.Position,
            PublicationPosition = tour.IsPublished ? tour.Position : 0,
            Position = tour.Position,
            UpdatedAt = tour.UpdatedAt
        };
    }

    private static CatalogTourDraftReadModel ToReadModel(CatalogTourReadModelEntity tour)
    {
        return new CatalogTourDraftReadModel(
            tour.CatalogTourId,
            tour.AdminTourId,
            tour.Identifier,
            tour.Title,
            tour.Slug,
            tour.IsPublished && tour.StreamVersion > 1 && !string.IsNullOrEmpty(tour.Summary),
            tour.Position,
            tour.UpdatedAt)
        {
            Summary = tour.Summary,
            Description = tour.Description,
            Itinerary = tour.Itinerary,
            SeoTitle = tour.SeoTitle,
            SeoDescription = tour.SeoDescription,
            StreamVersion = tour.StreamVersion
        };
    }

    private async ValueTask<bool> SaveProjection(string? slug, CancellationToken ct)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException exception) when (
            slug is not null
            && exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new CatalogTourSlugConflictException(
                $"The public tour slug '{slug}' is already in use.",
                exception);
        }
    }
}
