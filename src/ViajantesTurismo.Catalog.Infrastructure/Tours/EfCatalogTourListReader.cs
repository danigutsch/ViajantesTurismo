using Microsoft.EntityFrameworkCore;
using PandaBear.BuildingBlocks;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Reads summary pages from the Catalog tour projection.</summary>
internal sealed class EfCatalogTourListReader(CatalogDbContext dbContext) : ICatalogTourListReader
{
    /// <inheritdoc />
    public async ValueTask<CatalogTourPageDto> ListPage(
        string? page, CatalogTourListSort sort, bool descending, CancellationToken ct)
    {
        var tours = dbContext.CatalogTourReadModels.AsNoTracking();
        var total = await tours.CountAsync(ct).ConfigureAwait(false);
        var window = PageWindow.Create(page, CatalogTourPageDto.ItemsPerPage, total);
        var orderedTours = (sort, descending) switch
        {
            (CatalogTourListSort.Identifier, false) => tours.OrderBy(tour => tour.Identifier),
            (CatalogTourListSort.Identifier, true) => tours.OrderByDescending(tour => tour.Identifier),
            (CatalogTourListSort.Slug, false) => tours.OrderBy(tour => tour.Slug),
            (CatalogTourListSort.Slug, true) => tours.OrderByDescending(tour => tour.Slug),
            (CatalogTourListSort.IsPublished, false) => tours.OrderBy(tour => tour.IsPublished),
            (CatalogTourListSort.IsPublished, true) => tours.OrderByDescending(tour => tour.IsPublished),
            (_, true) => tours.OrderByDescending(tour => tour.Title),
            _ => tours.OrderBy(tour => tour.Title),
        };
        var items = await orderedTours
            .ThenBy(tour => tour.CatalogTourId)
            .Skip(window.Skip)
            .Take(window.Take)
            .Select(tour => new CatalogTourListItemDto(
                tour.CatalogTourId, tour.Identifier, tour.Title, tour.Slug, tour.IsPublished, tour.UpdatedAt))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        return new CatalogTourPageDto(items, total);
    }
}
