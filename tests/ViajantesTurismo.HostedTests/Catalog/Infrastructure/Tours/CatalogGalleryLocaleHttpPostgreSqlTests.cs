using System.Net;
using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies actual anonymous Gallery routes against the persisted review and placement state.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryLocaleHttpPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Localized_album_hides_pending_text_and_direct_media_requires_applied_placement()
    {
        // Arrange
        const string objectKey = "locale-640.jpg";
        const string englishAlt = "Cyclists at the river";
        const string englishCaption = "The river crossing";
        const string draftCaption = "Rascunho privado";
        const string reviewedAlt = "Ciclistas ao lado do rio";
        const string reviewedCaption = "A travessia do rio";
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedPublishableDraft(ct);
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "locale.jpg", objectKey, "sha256:locale", englishAlt, 0, true);
        var english = PublicContentCultures.EnglishUnitedStates;
        var portuguese = PublicContentCultures.PortugueseBrazil;
        photo.SetReviewedAccessibilityText(english, englishAlt, englishCaption, false).IsSuccess.ShouldBeTrue();
        await scenario.Images.Upsert(photo, ct);
        var published = await scenario.Writer.Publish(tour.Id, 2, ct);
        published.IsSuccess.ShouldBeTrue();
        await scenario.Runner.Project(ct);
        var placement = await scenario.Writer.ChangeGallery(tour.Id, [photo.Id], photo.Id, 3, canPublish: true, ct);
        placement.IsSuccess.ShouldBeTrue();

        var objects = MediaObjectStoreTestFactory.Create();
        objects.Seed(objectKey, "stored rendition"u8, "image/jpeg", "sha256:locale");
        await using var host = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), new TestPublicMediaImageStore(), objects);
        await using var connected = CatalogPublicTourBrowsingTestHost.Connect(host, database.ConnectionString);
        connected.UseKestrel(0);
        using var http = connected.CreateClient();
        http.DefaultRequestHeaders.Authorization = null;
        var englishPath = new Uri($"/api/v1/public/catalog/gallery/{tour.Slug}?culture=en-US", UriKind.Relative);
        var portuguesePath = new Uri($"/api/v1/public/catalog/gallery/{tour.Slug}?culture=pt-BR", UriKind.Relative);
        var renditionPath = new Uri($"/api/v1/public/catalog/media/{photo.Id}/640/jpg", UriKind.Relative);

        // Act
        using var fallback = await http.GetAsync(portuguesePath, ct);
        var fallbackJson = await fallback.Content.ReadAsStringAsync(ct);
        photo.SetDraftAccessibilityText(portuguese, "Descrição pendente", draftCaption).IsSuccess.ShouldBeTrue();
        await scenario.Images.Upsert(photo, ct);
        using var pending = await http.GetAsync(portuguesePath, ct);
        var pendingBody = await pending.Content.ReadAsStringAsync(ct);
        using var stillEnglish = await http.GetAsync(englishPath, ct);
        using var stillPublic = await http.GetAsync(renditionPath, HttpCompletionOption.ResponseHeadersRead, ct);

        photo.SetReviewedAccessibilityText(portuguese, reviewedAlt, reviewedCaption, false).IsSuccess.ShouldBeTrue();
        await scenario.Images.Upsert(photo, ct);
        using var reviewed = await http.GetAsync(portuguesePath, ct);
        var reviewedJson = await reviewed.Content.ReadAsStringAsync(ct);

        var removal = await scenario.Writer.ChangeGallery(tour.Id, [], null, 4, canPublish: true, ct);
        using var removedAlbum = await http.GetAsync(portuguesePath, ct);
        using var removedMedia = await http.GetAsync(renditionPath, HttpCompletionOption.ResponseHeadersRead, ct);
        var objectRemains = await objects.Exists(objectKey, ct);

        // Assert
        fallback.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fallbackPhoto = JsonSerializer.Deserialize<CatalogGalleryAlbumDto>(fallbackJson, JsonSerializerOptions.Web)
            .ShouldNotBeNull().Photos.ShouldHaveSingleItem();
        fallbackPhoto.Id.ShouldBe(photo.Id);
        fallbackPhoto.TextLanguage.ShouldBe(english.Value);
        fallbackPhoto.AltText.ShouldBe(englishAlt);
        fallbackPhoto.Caption.ShouldBe(englishCaption);

        pending.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        pendingBody.ShouldNotContain(draftCaption, StringComparison.Ordinal);
        stillEnglish.StatusCode.ShouldBe(HttpStatusCode.OK);
        stillPublic.StatusCode.ShouldBe(HttpStatusCode.OK);
        stillPublic.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("image/jpeg");

        reviewed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var reviewedPhoto = JsonSerializer.Deserialize<CatalogGalleryAlbumDto>(reviewedJson, JsonSerializerOptions.Web)
            .ShouldNotBeNull().Photos.ShouldHaveSingleItem();
        reviewedPhoto.Id.ShouldBe(photo.Id);
        reviewedPhoto.AltText.ShouldBe(reviewedAlt);
        reviewedPhoto.Caption.ShouldBe(reviewedCaption);
        reviewedJson.ShouldNotContain("\"textLanguage\"", StringComparison.Ordinal);

        removal.IsSuccess.ShouldBeTrue();
        removedAlbum.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        removedMedia.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        objectRemains.ShouldBeTrue();
        foreach (var response in new[] { fallback, pending, stillEnglish, stillPublic, reviewed, removedAlbum, removedMedia })
        {
            response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        }
    }
}
