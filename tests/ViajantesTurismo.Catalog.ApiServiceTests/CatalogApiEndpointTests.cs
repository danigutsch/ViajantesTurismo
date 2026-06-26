using System.Net.Http.Json;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(TestTraits.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraits.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiEndpointTests
{
    [Fact]
    public void Catalog_Api_Marker_Exposes_Entry_Assembly()
    {
        // Arrange
        var marker = new CatalogApiEntryPoint();

        // Act
        var entryPointAssembly = typeof(CatalogApiEntryPoint).Assembly;
        var markerAssembly = marker.Assembly;

        // Assert
        Assert.Same(CatalogApiMarker.Assembly, entryPointAssembly);
        Assert.Same(entryPointAssembly, markerAssembly);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Default_Health_Endpoint_Returns_Success(string path)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Production_Default_Health_Endpoint_Is_Not_Exposed(string path)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Public_Content_Endpoint_Saves_Review_Required_Draft()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs,
            EnUs = new PublicContentVariantDto
            {
                Language = PublicContentLanguageDto.EnUs,
                Title = "Welcome",
                Body = "Ride with us"
            },
            PtBr = new PublicContentVariantDto
            {
                Language = PublicContentLanguageDto.PtBr,
                Title = "Bem-vindo",
                Body = "Pedale conosco",
                RequiresHumanReview = true
            }
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<PublicContentDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("home.hero", saved.Key);
        Assert.True(saved.PtBr.RequiresHumanReview);
        Assert.Equal("ReviewRequired", saved.PublicationState);
    }
}
