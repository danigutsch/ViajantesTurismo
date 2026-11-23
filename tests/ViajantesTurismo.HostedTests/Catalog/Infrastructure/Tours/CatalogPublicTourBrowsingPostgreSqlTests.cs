using System.Net;
using Microsoft.EntityFrameworkCore;
using PandaBear.HttpClients;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies the public tour-card projections against a large persisted Catalog.</summary>
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogPublicTourBrowsingPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Visible_tour_pages_keep_stable_order_and_load_only_the_selected_card_cover()
    {
        // Arrange
        const int visibleCount = 803;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var context = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(database.ConnectionString);
        await context.Database.MigrateAsync(ct);
        var rows = Enumerable.Range(0, visibleCount + 2).Select(index => new CatalogTourReadModelEntity
        {
            CatalogTourId = Guid.Parse(FormattableString.Invariant($"00000000-0000-0000-0000-{index + 1:D12}")),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = FormattableString.Invariant($"BROWSE-{index:D3}"),
            Title = "Same public tour title",
            Slug = FormattableString.Invariant($"browse-tour-{visibleCount + 1 - index:D3}"),
            Summary = "Ride with the group.",
            IsPublished = index != visibleCount,
            UpdatedAt = DateTimeOffset.UnixEpoch
        }).ToArray();
        var retired = rows[^1];
        context.CatalogTourReadModels.AddRange(rows);
        context.CatalogTourRetirements.Add(new CatalogTourRetirementEntity
        {
            AdminTourId = retired.AdminTourId,
            RetirementId = Guid.CreateVersion7(),
            WithdrawnAt = DateTimeOffset.UnixEpoch
        });
        var oldCover = PublicMediaImageTestFactory.CreateReadyImage(rows[0].CatalogTourId, "legacy.jpg", "legacy-640.jpg", "sha256:legacy", "Legacy cover", 0, true);
        var authoredCover = PublicMediaImageTestFactory.CreateReadyImage(rows[0].CatalogTourId, "authored.jpg", "authored-640.jpg", "sha256:authored", "Authored cover", 1, false);
        rows[0].GalleryPhotoIds = [oldCover.Id, authoredCover.Id];
        rows[0].GalleryCoverId = authoredCover.Id;
        var legacyPhotos = Enumerable.Range(0, 803).Select(index => PublicMediaImageTestFactory.CreateReadyImage(
            rows[1].CatalogTourId, FormattableString.Invariant($"stage-{index}.jpg"), FormattableString.Invariant($"stage-{index}-640.jpg"),
            FormattableString.Invariant($"sha256:stage-{index}"), FormattableString.Invariant($"Stage {index}"), index, index == 29)).ToArray();
        context.PublicMediaImages.AddRange([oldCover, authoredCover, .. legacyPhotos]);
        await context.SaveChangesAsync(ct);
        await using var host = CatalogApiTestHost.CreateAnonymous();
        await using var connected = CatalogPublicTourBrowsingTestHost.Connect(host, database.ConnectionString);
        using var http = connected.CreateClient();
        var catalog = new PublicCatalogApiClient(http);

        // Act
        using var defaultPage = await http.GetAsync(new Uri("/api/v1/public/catalog/tours/browse/page", UriKind.Relative), ct);
        var first = await catalog.GetPublishedTourPage(null, PublishedTourPageDto.IndexPageSize, ct);
        var next = await catalog.GetPublishedTourPage("2", PublishedTourPageDto.IndexPageSize, ct);
        var last = await catalog.GetPublishedTourPage("67", PublishedTourPageDto.IndexPageSize, ct);
        var preview = await catalog.GetPublishedTourPage(null, PublishedTourPageDto.HomePreviewSize, ct);
        var missing = await catalog.GetPublishedTourPage("68", PublishedTourPageDto.IndexPageSize, ct);
        var invalidSize = await catalog.GetPublishedTourPage(null, 13, ct);
        var detail = await catalog.GetPublishedTourDetailsPage(rows[1].Slug, null, ct);
        var laterPhotos = await catalog.GetPublishedTourDetailsPage(rows[1].Slug, "2", ct);
        var lastPhotos = await catalog.GetPublishedTourDetailsPage(rows[1].Slug, "134", ct);
        var missingPhotos = await catalog.GetPublishedTourDetailsPage(rows[1].Slug, "135", ct);
        var legacyDetail = await catalog.GetPublishedTourBySlug(rows[1].Slug, ct);
        var retiredDetail = await catalog.GetPublishedTourDetailsPage(retired.Slug, null, ct);
        var authored = await catalog.GetPublishedTourDetailsPage(rows[0].Slug, null, ct);
        var galleryless = await catalog.GetPublishedTourDetailsPage(rows[2].Slug, null, ct);

        // Assert
        var firstPage = first.Value.ShouldNotBeNull();
        defaultPage.StatusCode.ShouldBe(HttpStatusCode.OK);
        defaultPage.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        firstPage.Total.ShouldBe(visibleCount);
        firstPage.Tours.Select(tour => tour.Slug).ShouldBe(rows.Take(12).Select(tour => tour.Slug));
        firstPage.Tours[0].Images.ShouldHaveSingleItem().Id.ShouldBe(authoredCover.Id);
        firstPage.Tours[1].Images.ShouldHaveSingleItem().Id.ShouldBe(legacyPhotos[29].Id);
        firstPage.Tours[2].Images.ShouldBeEmpty();
        firstPage.Tours[0].Links.ShouldHaveSingleItem().Href.ShouldBe($"/api/v1/public/catalog/tours/{rows[0].Slug}");
        next.Value.ShouldNotBeNull().Tours.Select(tour => tour.Slug).ShouldBe(rows.Skip(12).Take(12).Select(tour => tour.Slug));
        last.Value.ShouldNotBeNull().Tours.Select(tour => tour.Slug).ShouldBe(rows.Skip(792).Take(11).Select(tour => tour.Slug));
        preview.Value.ShouldNotBeNull().Tours.Select(tour => tour.Slug).ShouldBe(rows.Take(6).Select(tour => tour.Slug));
        missing.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        invalidSize.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var photoWindow = detail.Value.ShouldNotBeNull();
        photoWindow.Total.ShouldBe(802);
        photoWindow.Tour.Images.Select(photo => photo.Id).ShouldBe([legacyPhotos[29].Id, .. legacyPhotos.Take(6).Select(photo => photo.Id)]);
        var nextPhotoWindow = laterPhotos.Value.ShouldNotBeNull();
        nextPhotoWindow.Page.ShouldBe(2);
        nextPhotoWindow.Tour.Images.Select(photo => photo.Id).ShouldBe([legacyPhotos[29].Id, .. legacyPhotos.Skip(6).Take(6).Select(photo => photo.Id)]);
        var lastPhotoWindow = lastPhotos.Value.ShouldNotBeNull();
        lastPhotoWindow.Page.ShouldBe(134);
        lastPhotoWindow.Tour.Images.Select(photo => photo.Id).ShouldBe([legacyPhotos[29].Id, .. legacyPhotos.TakeLast(4).Select(photo => photo.Id)]);
        missingPhotos.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        retiredDetail.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        legacyDetail.Value.ShouldNotBeNull().Images.Count.ShouldBe(803);
        authored.Value.ShouldNotBeNull().Tour.Images.Select(photo => photo.Id).ShouldBe([authoredCover.Id, oldCover.Id]);
        galleryless.Value.ShouldNotBeNull().Tour.Images.ShouldBeEmpty();
    }
}
