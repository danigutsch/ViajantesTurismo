using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiClientIntegrationTests
{
    [Fact]
    public async Task Catalog_tours_clients_update_publish_and_unpublish_through_api_host()
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
                DateTimeOffset.UtcNow),
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
        var saved = await managementClient.UpdatePresentation(tourId, request, cancellationToken);
        await managementClient.Publish(
            tourId,
            new CatalogTourPublicationRequest { ExpectedVersion = 2 },
            cancellationToken);
        var published = await publicClient.GetPublishedTourBySlug("contract-client-tour", cancellationToken);
        await managementClient.Unpublish(
            tourId,
            new CatalogTourPublicationRequest { ExpectedVersion = 3 },
            cancellationToken);
        var unpublished = await publicClient.GetPublishedTourBySlug("contract-client-tour", cancellationToken);

        // Assert
        saved.ShouldNotBeNull();
        saved.Title.ShouldBe("Contract Client Tour");
        saved.IsPublished.ShouldBeFalse();
        published.ShouldNotBeNull();
        published.Title.ShouldBe("Contract Client Tour");
        published.Slug.ShouldBe("contract-client-tour");
        published.Summary.ShouldBe("A contract-tested tour.");
        unpublished.ShouldBeNull();
    }

    [Fact]
    public async Task Public_content_client_saves_and_reads_content_through_api_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = CatalogApiTestHost.Create();
        using var httpClient = factory.CreateClient();
        var sut = new PublicContentApiClient(httpClient);
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco" });

        // Act
        var saved = await sut.SaveContent("home.hero", request, cancellationToken);
        var read = await sut.GetContent("home.hero", cancellationToken);

        // Assert
        saved.Key.ShouldBe("HOME.HERO");
        read.ShouldNotBeNull();
        read.Key.ShouldBe("HOME.HERO");
        read.Variants.ShouldContain(variant => variant.Language == PublicContentLanguageDto.PtBr && variant.Title == "Bem-vindo");
    }

}
