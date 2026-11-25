using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.BuildingBlocks;
using SharedKernel.Testing;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Exercises one pending gallery read through PostgreSQL, Catalog HTTP and Management prerendering.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class GalleryReviewPrerenderTests(HostedTestFixture fixture)
{
    [Fact]
    public async Task A_large_gallery_prerenders_one_pending_cover_and_advances_after_review()
    {
        // Arrange
        const int imageCount = 160;
        const string coverText = "Portuguese pending cover description";
        const string nextText = "English pending image description";
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var portuguese = ContentCulture.Restore(PublicContentLanguageTags.PortugueseBrazil);
        var english = ContentCulture.Restore(PublicContentLanguageTags.EnglishUnitedStates);
        await using var database = await fixture.Databases.Create(ct);
        await using var seed = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        seed.Database.SetConnectionString(database.ConnectionString);
        await seed.Database.MigrateAsync(ct);
        var images = Enumerable.Range(0, imageCount).Select(index =>
            PublicMediaImageTestFactory.CreateReadyImage(tourId, $"source-{index}.jpg", $"variant-{index}.jpg",
                $"sha256:gallery-{index}", $"Reviewed image {index}", index, isCover: index == imageCount - 1)).ToArray();
        var cover = images[^1];
        cover.SetAiDraftAccessibilityText(portuguese, coverText, caption: null).IsSuccess.ShouldBeTrue();
        var nextExpected = images[10];
        nextExpected.SetDraftAccessibilityText(english, nextText, caption: null).IsSuccess.ShouldBeTrue();
        images[20].SetAiDraftAccessibilityText(portuguese, "Later pending description", caption: null).IsSuccess.ShouldBeTrue();
        var otherTourImage = PublicMediaImageTestFactory.CreateReadyImage(Guid.CreateVersion7(), "other.jpg", "other-640.jpg",
            "sha256:other-gallery", "Unrelated gallery image", 0, isCover: true);
        otherTourImage.SetDraftAccessibilityText(english, "Unrelated pending description", caption: null).IsSuccess.ShouldBeTrue();
        seed.PublicMediaImages.AddRange(images);
        seed.PublicMediaImages.Add(otherTourImage);
        await seed.SaveChangesAsync(ct);

        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(
            tourId, Guid.CreateVersion7(), "REVIEW-TOUR", "Review Tour", "review-tour", false, 1, DateTimeOffset.UnixEpoch), ct);
        var probe = new GalleryReviewQueryProbe();
        var readOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(database.ConnectionString)
            .AddInterceptors(probe)
            .Options;
        await using var baseApi = CatalogApiTestHost.Create(tours, new TestPublicContentStore());
        await using var catalogApi = baseApi.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Scoped(_ => new CatalogDbContext(readOptions)));
            services.Replace(ServiceDescriptor.Scoped<IPublicMediaImageStore, EfPublicMediaImageStore>());
        }));
        using var catalogHttp = catalogApi.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(catalogHttp, "Operator");
        var catalog = new CatalogToursApiClient(catalogHttp);
        await using var management = await ManagementWebBrowserHost.StartWithClients(ct, catalog: catalog);
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(15) };

        // Act
        var html = await browser.GetStringAsync(new Uri($"/catalog/tours/{tourId}/images/review", UriKind.Relative), ct);
        var readsDuringPrerender = probe.ImageReads;
        var first = await catalog.GetNextPendingReviewImage(tourId, ct);
        cover.SetReviewedAccessibilityText(portuguese, coverText, caption: null, isDecorative: false).IsSuccess.ShouldBeTrue();
        seed.ChangeTracker.Clear();
        await new EfPublicMediaImageStore(seed).Upsert(cover, ct);
        var afterReview = await catalog.GetNextPendingReviewImage(tourId, ct);

        // Assert
        html.ShouldContain("Review Image Accessibility", StringComparison.Ordinal);
        html.ShouldContain(coverText, StringComparison.Ordinal);
        html.ShouldNotContain(nextText, StringComparison.Ordinal);
        html.ShouldNotContain("Unrelated pending description", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
        readsDuringPrerender.ShouldBe(1);
        var initial = first.Value.ShouldNotBeNull().Image.ShouldNotBeNull();
        initial.Id.ShouldBe(cover.Id);
        initial.AccessibilityTexts.ShouldContain(text => text.Language == PublicContentLanguageTags.PortugueseBrazil && text.RequiresHumanReview);
        initial.ResponsiveVariants.Count.ShouldBe(1);
        afterReview.Value.ShouldNotBeNull().Image.ShouldNotBeNull().Id.ShouldBe(nextExpected.Id);
        probe.ImageReads.ShouldBe(3);
        probe.LimitedImageReads.ShouldBe(3);
        probe.MaterializedImages.ShouldBe(3);
    }
}
