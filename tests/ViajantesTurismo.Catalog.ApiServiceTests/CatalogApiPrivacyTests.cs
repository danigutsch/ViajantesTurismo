using System.Net.Http.Json;
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
}
