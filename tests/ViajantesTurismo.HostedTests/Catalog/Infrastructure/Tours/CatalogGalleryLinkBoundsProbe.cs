using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Measures placements materialized by public photo reads for a widely shared image.</summary>
internal sealed class CatalogGalleryLinkBoundsProbe : IMaterializationInterceptor
{
    private const int LinkedTourCount = 803;
    private readonly List<Guid> materialized = [];

    /// <summary>Seeds a shared photo and reads the index, an album and the tour's photo page.</summary>
    /// <param name="connectionString">The dedicated PostgreSQL fixture connection.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The public photo results and their expected and materialized placement identifiers.</returns>
    internal static async Task<(CatalogGalleryIndexDto Index, CatalogGalleryAlbumDto Album, Guid SharedPhoto,
        Guid[] ExpectedIndexTours, Guid[] IndexLinks, Guid AlbumTour, Guid[] AlbumLinks,
        IReadOnlyList<CatalogTourImageDto> DetailImages, Guid[] DetailLinks)> Read(
        string connectionString, CancellationToken ct)
    {
        await using var seed = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        seed.Database.SetConnectionString(connectionString);
        await seed.Database.MigrateAsync(ct);

        var tours = Enumerable.Range(0, LinkedTourCount).Select(index => new CatalogTourReadModelEntity
        {
            CatalogTourId = Guid.CreateVersion7(),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = $"Tour-{index:D3}",
            Slug = $"shared-tour-{index:D3}",
            Title = $"Shared tour {index:D3}",
            IsPublished = true
        }).ToArray();
        seed.CatalogTourReadModels.AddRange(tours);
        var image = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = Guid.CreateVersion7(),
                SourceObjectKey = "shared-source.jpg",
                Checksum = "sha256:shared-gallery",
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = MediaImageProcessingStatus.Ready,
                AltText = "Shared Catalog photo"
            },
            [new MediaImageResponsiveVariant("shared-640.jpg", 640, 427, "image/jpeg", 1024)],
            [],
            [.. tours.Select(tour => new MediaImageTourLink(tour.CatalogTourId, 0, true))]).Value;
        seed.PublicMediaImages.Add(image);
        await seed.SaveChangesAsync(ct);

        var probe = new CatalogGalleryLinkBoundsProbe();
        var options = new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString)
            .AddInterceptors(probe).Options;
        await using var context = new CatalogDbContext(options);
        var store = new EfCatalogGalleryReadStore(context);
        var culture = PublicContentCultures.Supported.Default;
        var index = await store.ListAlbums(culture, null, ct)
            ?? throw new InvalidOperationException("The shared image should produce visible albums.");
        Guid[] indexLinks = [.. probe.materialized];
        probe.materialized.Clear();

        var target = tours[1];
        var album = await store.GetAlbum(target.Slug, culture, null, ct)
            ?? throw new InvalidOperationException("The second tour should retain the shared image.");
        Guid[] albumLinks = [.. probe.materialized];
        probe.materialized.Clear();

        var tour = await new EfCatalogTourReadModelStore(context).GetPublishedTourBySlug(target.Slug, ct)
            ?? throw new InvalidOperationException("The second tour should be publicly visible.");
        var (_, detailImages) = await new EfCatalogTourListReader(context).ListPublishedPhotos(tour, null, ct)
            ?? throw new InvalidOperationException("The second tour should have a public photo page.");
        return (index, album, image.Id, [.. tours.Take(index.PageSize).Select(tour => tour.CatalogTourId)],
            indexLinks, target.CatalogTourId, albumLinks, detailImages, [.. probe.materialized]);
    }

    /// <inheritdoc />
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (entity is MediaImageTourLink link)
        {
            materialized.Add(link.CatalogTourId);
        }

        return entity;
    }
}
