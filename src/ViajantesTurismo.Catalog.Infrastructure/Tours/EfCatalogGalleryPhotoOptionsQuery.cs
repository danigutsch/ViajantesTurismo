using Microsoft.EntityFrameworkCore;
using PandaBear.BuildingBlocks;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Searches management gallery photo choices in PostgreSQL.</summary>
internal sealed class EfCatalogGalleryPhotoOptionsQuery(CatalogDbContext dbContext) : ICatalogGalleryPhotoOptionsQuery
{
    private const int MaxLabelLength = 80;

    /// <inheritdoc />
    public async Task<CatalogGalleryPhotoOptionsDto?> Search(
        Guid catalogTourId, CatalogGalleryPhotoOptionsRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await dbContext.CatalogTourReadModels.AnyAsync(tour => tour.CatalogTourId == catalogTourId, ct).ConfigureAwait(false))
        {
            return null;
        }

        var photos = dbContext.PublicMediaImages.Where(image => image.TourLinks.Any(link => link.CatalogTourId == catalogTourId));
        if (request.IncludedPhotoIds is { } includedPhotoIds)
        {
            if (includedPhotoIds.Count == 0)
            {
                return new CatalogGalleryPhotoOptionsDto([], 0);
            }

            var ids = includedPhotoIds.ToArray();
            photos = photos.Where(image => ids.Contains(image.Id));
        }

        var key = SearchKey.Of(request.Query);
        if (Guid.TryParse(key, out var photoId))
        {
            photos = photos.Where(image => image.Id == photoId);
        }
        else if (key.Length > 0)
        {
            var escaped = key.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            var pattern = $"%{escaped}%";
            photos = photos.Where(image =>
                EF.Functions.ILike(EF.Functions.Unaccent(image.AltText), EF.Functions.Unaccent(pattern), "\\")
                || (image.Caption != null && EF.Functions.ILike(EF.Functions.Unaccent(image.Caption), EF.Functions.Unaccent(pattern), "\\")));
        }

        var total = await photos.CountAsync(ct).ConfigureAwait(false);
        var page = await photos
            .OrderBy(image => image.TourLinks.Where(link => link.CatalogTourId == catalogTourId).Min(link => link.DisplayOrder))
            .ThenBy(image => image.Id)
            .Take(Math.Clamp(request.Limit, 1, CatalogGalleryPhotoOptionsRequest.MaxPageSize))
            .Select(image => new
            {
                image.Id,
                image.AltText,
                image.Caption,
                Preview = image.ResponsiveVariants
                    .Where(variant => image.ProcessingStatus == MediaImageProcessingStatus.Ready)
                    .OrderBy(variant => variant.Width)
                    .ThenBy(variant => variant.SortOrder)
                    .Select(variant => new { variant.Width, variant.ContentType })
                    .FirstOrDefault()
            })
            .ToArrayAsync(ct).ConfigureAwait(false);

        var choices = page.Select(image => new CatalogGalleryPhotoOptionDto(
            image.Id, Label(image.Id, image.AltText, image.Caption), image.Preview?.Width, image.Preview?.ContentType)).ToArray();
        return new CatalogGalleryPhotoOptionsDto(choices, Math.Max(total, choices.Length));
    }

    private static string Label(Guid id, string altText, string? caption)
    {
        var label = !string.IsNullOrWhiteSpace(altText) ? altText : caption;
        if (string.IsNullOrWhiteSpace(label))
        {
            return $"Photo {id:D}";
        }

        if (label.Length <= MaxLabelLength)
        {
            return label;
        }

        var end = char.IsHighSurrogate(label[MaxLabelLength - 1]) ? MaxLabelLength - 1 : MaxLabelLength;
        return string.Concat(label.AsSpan(0, end), "…");
    }
}
