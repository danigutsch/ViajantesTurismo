using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class EfCatalogTourReadModelStore(CatalogDbContext dbContext) : ICatalogTourReadModelStore
{
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
        existing.Position = tour.Position;
        existing.UpdatedAt = tour.UpdatedAt;

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(
        Guid catalogTourId,
        CatalogTourPresentationUpdate update,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);

        var existing = await dbContext.CatalogTourReadModels
            .SingleOrDefaultAsync(current => current.CatalogTourId == catalogTourId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return null;
        }

        existing.Title = StringSanitizer.Sanitize(update.Title) ?? string.Empty;
        existing.Slug = StringSanitizer.Sanitize(update.Slug) ?? string.Empty;
        existing.IsPublished = update.IsPublished;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToReadModel(existing);
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

        var sanitizedSlug = StringSanitizer.Sanitize(slug) ?? string.Empty;

        var tour = await dbContext.CatalogTourReadModels
            .Where(tour => tour.IsPublished)
            .SingleOrDefaultAsync(tour => tour.Slug == sanitizedSlug, ct)
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
            IsPublished = tour.IsPublished,
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
            tour.IsPublished,
            tour.Position,
            tour.UpdatedAt);
    }
}
