using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Infrastructure.Media;

/// <summary>Queries rendition-ready media with reviewed default accessibility text.</summary>
internal static class CatalogPublicMediaQueries
{
    /// <summary>Mirrors the domain's default eligibility rule before projecting public media in PostgreSQL.</summary>
    public static IQueryable<PublicMediaImage> Ready(CatalogDbContext context) =>
        context.PublicMediaImages.AsNoTracking().Where(photo => photo.ProcessingStatus == MediaImageProcessingStatus.Ready
            && photo.ResponsiveVariants.Any() && !photo.RequiresHumanReview && (photo.IsDecorative || !string.IsNullOrWhiteSpace(photo.AltText)));
}
