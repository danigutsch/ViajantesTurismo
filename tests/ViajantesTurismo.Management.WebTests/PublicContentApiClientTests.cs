using System.Net;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

public sealed class PublicContentApiClientTests
{
    [Fact]
    public async Task GetContent_Requests_Management_Public_Content_Endpoint_And_Skips_Null_Items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                [
                  {
                    "key":"home.hero",
                    "sourceLanguage":1,
                    "enUs":{"language":1,"title":"Welcome","body":"Ride with us","requiresHumanReview":false},
                    "ptBr":{"language":2,"title":"Bem-vindo","body":"Pedale conosco","requiresHumanReview":true},
                    "publicationState":"ReviewRequired"
                  },
                  null
                ]
                """);
        });
        var sut = new PublicContentApiClient(httpClient);

        // Act
        var content = await sut.GetContent(Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/catalog/public-content", requestPath);
        var entry = Assert.Single(content);
        Assert.Equal("home.hero", entry.Key);
    }

    [Fact]
    public async Task GetContent_By_Key_Returns_Null_When_Endpoint_Returns_Not_Found()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new PublicContentApiClient(httpClient);

        // Act
        var content = await sut.GetContent("home.hero", Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(content);
    }

    [Fact]
    public async Task SaveContent_Sends_Upsert_Request_To_Keyed_Endpoint()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "key":"home.hero",
                  "sourceLanguage":1,
                  "enUs":{"language":1,"title":"Welcome","body":"Ride with us","requiresHumanReview":false},
                  "ptBr":{"language":2,"title":"Bem-vindo","body":"Pedale conosco","requiresHumanReview":true},
                  "publicationState":"ReviewRequired"
                }
                """);
        });
        var sut = new PublicContentApiClient(httpClient);
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs,
            EnUs = new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" },
            PtBr = new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco", RequiresHumanReview = true }
        };

        // Act
        var saved = await sut.SaveContent("home.hero", request, Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("PUT", requestMethod);
        Assert.Equal("/catalog/public-content/home.hero", requestPath);
        Assert.Equal("ReviewRequired", saved.PublicationState);
    }

}
