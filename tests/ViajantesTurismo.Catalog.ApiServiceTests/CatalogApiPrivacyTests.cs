using System.Net.Http.Json;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiPrivacyTests
{
    [Fact]
    public async Task Presentation_conflict_does_not_expose_exception_message()
    {
        // Arrange
        const string sensitiveMessage = "traveler@example.com cannot edit customer-123";
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            CatalogApiCachingTestData.CreatePublishedTour(tourId, "Privacy Tour", "privacy-tour"),
            TestContext.Current.CancellationToken);
        var eventStore = new TestEventStore
        {
            LoadException = new CatalogTourPublishedPresentationChangeException(sensitiveMessage)
        };
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), eventStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Privacy Tour",
                Slug = "privacy-tour",
                Summary = "Safe summary.",
                ExpectedVersion = 1
            },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        body.ShouldNotContain(sensitiveMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publication_validation_does_not_expose_exception_message()
    {
        // Arrange
        const string sensitiveMessage = "traveler@example.com is linked to booking-123";
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            CatalogApiCachingTestData.CreatePublishedTour(tourId, "Privacy Tour", "privacy-tour"),
            TestContext.Current.CancellationToken);
        var eventStore = new TestEventStore
        {
            LoadException = new CatalogTourPublicationNotReadyException(sensitiveMessage)
        };
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), eventStore);
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
