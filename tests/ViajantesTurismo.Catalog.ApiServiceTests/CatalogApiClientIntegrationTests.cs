using SharedKernel.HttpClients;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiClientIntegrationTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    public async Task Public_content_overview_requires_catalog_read(string? role, HttpStatusCode expectedStatus)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var http = factory.CreateClient();
        if (role is not null)
        {
            CatalogApiTestHost.ConfigureAuthenticatedClient(http, role);
        }

        // Act
        using var response = await http.GetAsync(new Uri("/api/v1/catalog/public-content-overview", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
    }

    [Fact]
    public async Task Catalog_tours_clients_update_publish_and_unpublish_through_API_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "CLIENT-TOUR-1",
                "Draft Tour",
                "draft-tour",
                false,
                1,
                TimeProvider.System.GetUtcNow()),
            cancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var httpClient = factory.CreateClient();
        var managementClient = new CatalogToursApiClient(httpClient);
        var publicClient = new PublicCatalogApiClient(httpClient);
        var request = new UpsertCatalogTourPresentationRequest
        {
            Title = "Contract Client Tour",
            Slug = "contract-client-tour",
            Summary = "A contract-tested tour.",
            Description = "A detailed contract-tested tour description.",
            Itinerary = "Day one: test the HTTP contract.",
            SeoTitle = "Contract Client Tour SEO",
            SeoDescription = "Contract-tested search description.",
            ExpectedVersion = 1
        };

        // Act
        var savedOutcome = await managementClient.UpdatePresentation(tourId, request, cancellationToken);
        var managementItem = await managementClient.GetTour(tourId, cancellationToken);
        var managementList = await managementClient.GetTours(cancellationToken);
        var publishOutcome = await managementClient.Publish(
            tourId,
            new CatalogTourPublicationRequest { ExpectedVersion = 2 },
            cancellationToken);
        var publishedOutcome = await publicClient.GetPublishedTourBySlug("contract-client-tour", cancellationToken);
        var unpublishOutcome = await managementClient.Unpublish(
            tourId,
            new CatalogTourPublicationRequest { ExpectedVersion = 3 },
            cancellationToken);
        var unpublishedOutcome = await publicClient.GetPublishedTourBySlug("contract-client-tour", cancellationToken);

        // Assert
        savedOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var saved = savedOutcome.Value.ShouldNotBeNull();
        saved.Title.ShouldBe("Contract Client Tour");
        saved.IsPublished.ShouldBeFalse();
        saved.Links.ShouldContain(link => link.Rel == "self" && link.Href == $"/api/v1/catalog/tours/{tourId}" && link.Method == "GET");
        saved.Links.ShouldContain(link => link.Rel == "images" && link.Href == $"/api/v1/catalog/tours/{tourId}/images" && link.Method == "GET");
        managementItem.Value.ShouldNotBeNull().Links.ShouldBe(saved.Links);
        managementList.Value.ShouldNotBeNull().ShouldHaveSingleItem().Links.ShouldBe(saved.Links);
        publishOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        publishedOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var published = publishedOutcome.Value.ShouldNotBeNull();
        published.Title.ShouldBe("Contract Client Tour");
        published.Slug.ShouldBe("contract-client-tour");
        published.Summary.ShouldBe("A contract-tested tour.");
        published.Links.ShouldContain(link => link.Rel == "self" && link.Href == "/api/v1/public/catalog/tours/contract-client-tour" && link.Method == "GET");
        unpublishOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        unpublishedOutcome.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        unpublishedOutcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task Public_content_client_saves_and_reads_content_through_API_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = CatalogApiTestHost.Create();
        using var httpClient = factory.CreateClient();
        var sut = new PublicContentApiClient(httpClient);
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageTags.EnglishUnitedStates
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageTags.EnglishUnitedStates, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageTags.PortugueseBrazil, Title = "Bem-vindo", Body = "Pedale conosco" });

        // Act
        var savedOutcome = await sut.SaveContent("home.hero", request, cancellationToken);
        var readOutcome = await sut.GetContent("home.hero", cancellationToken);
        var listOutcome = await sut.GetContent(cancellationToken);

        // Assert
        savedOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var saved = savedOutcome.Value.ShouldNotBeNull();
        saved.Key.ShouldBe("HOME.HERO");
        var self = saved.Links.ShouldHaveSingleItem();
        self.Rel.ShouldBe("self");
        self.Href.ShouldBe("/api/v1/catalog/public-content/HOME.HERO");
        self.Method.ShouldBe("GET");
        readOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var read = readOutcome.Value.ShouldNotBeNull();
        read.Key.ShouldBe("HOME.HERO");
        read.Links.ShouldBe(saved.Links);
        read.Variants.ShouldContain(variant => variant.Language == PublicContentLanguageTags.PortugueseBrazil && variant.Title == "Bem-vindo");
        listOutcome.Value.ShouldNotBeNull().ShouldHaveSingleItem().Links.ShouldBe(saved.Links);
    }

}
