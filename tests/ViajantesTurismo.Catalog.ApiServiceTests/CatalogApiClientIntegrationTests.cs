using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiClientIntegrationTests
{
    [Fact]
    public async Task Catalog_tours_clients_update_and_read_published_tour_through_api_host()
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
            IsPublished = true
        };

        // Act
        var saved = await managementClient.UpdatePresentation(tourId, request, cancellationToken);
        var published = await publicClient.GetPublishedTourBySlug("contract-client-tour", cancellationToken);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("Contract Client Tour", saved.Title);
        Assert.True(saved.IsPublished);
        Assert.NotNull(published);
        Assert.Equal(tourId, published.Id);
        Assert.Equal("contract-client-tour", published.Slug);
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
        Assert.Equal("HOME.HERO", saved.Key);
        Assert.NotNull(read);
        Assert.Equal("HOME.HERO", read.Key);
        Assert.Contains(read.Variants, variant => variant.Language == PublicContentLanguageDto.PtBr && variant.Title == "Bem-vindo");
    }

    [Fact]
    public async Task Public_theme_client_saves_and_reads_theme_through_api_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = CatalogApiTestHost.Create();
        using var httpClient = factory.CreateClient();
        var sut = new PublicThemeApiClient(httpClient);
        var request = new PublicThemeSettingsDto
        {
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        };

        // Act
        var saved = await sut.SaveTheme(request, cancellationToken);
        var read = await sut.GetTheme(cancellationToken);

        // Assert
        Assert.Equal("#112233", saved.PrimaryColor);
        Assert.Equal("#112233", read.PrimaryColor);
        Assert.Equal("Inter", read.HeadingFontFamily);
    }
}
