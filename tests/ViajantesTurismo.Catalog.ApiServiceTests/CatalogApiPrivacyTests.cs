using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiPrivacyTests
{
    [Fact]
    public async Task Presentation_conflict_does_not_expose_stored_tour_content()
    {
        // Arrange
        const string sensitiveMessage = "traveler@example.com cannot edit customer-123";
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            CatalogApiCachingTestData.CreatePublishedTour(tourId, sensitiveMessage, "privacy-tour"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Privacy Tour",
                Slug = "privacy-tour",
                Summary = "Safe summary.",
                ExpectedVersion = 3
            },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        body.ShouldNotContain(sensitiveMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publication_validation_does_not_expose_stored_tour_content()
    {
        // Arrange
        const string sensitiveMessage = "traveler@example.com is linked to booking-123";
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "PRIVACY-TOUR",
                sensitiveMessage,
                "privacy-tour",
                false,
                1,
                TimeProvider.System.GetUtcNow()),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/publish", UriKind.Relative),
            new CatalogTourPublicationRequest { ExpectedVersion = 1 },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        body.ShouldNotContain(sensitiveMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/v1/public/catalog/tours")]
    [InlineData("/api/v1/public/catalog/tours/privacy-tour")]
    public async Task Public_tour_payloads_do_not_expose_admin_only_customer_or_booking_fields(string path)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            CatalogApiCachingTestData.CreatePublishedTour(tourId, "Privacy Tour", "privacy-tour"),
            TestContext.Current.CancellationToken);
        var imageStore = new TestPublicMediaImageStore();
        await imageStore.Upsert(
            PublicMediaImageTestFactory.CreateReadyImage(tourId, "source.jpg", "public.jpg", "sha256:privacy", "Cyclists", 0, true),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), imageStore);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var propertyNames = JsonPropertyNames.Collect(body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        propertyNames.ShouldNotBeEmpty();
        AdminOnlyPiiFieldNames.Matching(propertyNames).ShouldBeEmpty();
    }

    [Fact]
    public async Task Public_content_payload_does_not_expose_admin_only_customer_or_booking_fields()
    {
        // Arrange
        var publicContentStore = new TestPublicContentStore();
        await publicContentStore.SaveContent(
            CatalogApiCachingTestData.CreatePublishedContent("Welcome"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/content/home.hero?culture=en-US", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var propertyNames = JsonPropertyNames.Collect(body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        propertyNames.ShouldNotBeEmpty();
        AdminOnlyPiiFieldNames.Matching(propertyNames).ShouldBeEmpty();
    }

    [Fact]
    public async Task Public_branding_payload_does_not_expose_admin_only_customer_or_booking_fields()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous(new TestBrandingSettingsStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/{BrandingRoutes.PublicSettingsPath}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var propertyNames = JsonPropertyNames.Collect(body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        propertyNames.ShouldNotBeEmpty();
        AdminOnlyPiiFieldNames.Matching(propertyNames).ShouldBeEmpty();
    }
}
