using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Common.Contracts;

namespace ViajantesTurismo.Management.WebTests;

public sealed class PublicContentApiClientTests
{
    [Fact]
    public async Task GetContent_requests_management_public_content_endpoint_and_skips_null_items()
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
                    "variants":[{"language":1,"title":"Welcome","body":"Ride with us","requiresHumanReview":false},{"language":2,"title":"Bem-vindo","body":"Pedale conosco","requiresHumanReview":true}],
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
        requestPath.ShouldBe("/catalog/public-content");
        var entry = content.ShouldHaveSingleItem();
        entry.Key.ShouldBe("home.hero");
    }

    [Fact]
    public async Task GetContent_by_key_returns_null_when_endpoint_returns_not_found()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new PublicContentApiClient(httpClient);

        // Act
        var content = await sut.GetContent("home.hero", Xunit.TestContext.Current.CancellationToken);

        // Assert
        content.ShouldBeNull();
    }

    [Theory]
    [InlineData("home/hero", "/catalog/public-content/home/hero")]
    [InlineData("/home//hero/", "/catalog/public-content/home/hero")]
    [InlineData("home / hero", "/catalog/public-content/home/hero")]
    public async Task GetContent_by_key_normalizes_and_escapes_key_segments(string key, string expectedPath)
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "key":"home.hero",
                  "sourceLanguage":1,
                  "variants":[{"language":1,"title":"Welcome","body":"Ride with us","requiresHumanReview":false}],
                  "publicationState":"Published"
                }
                """);
        });
        var sut = new PublicContentApiClient(httpClient);

        // Act
        var content = await sut.GetContent(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        content.ShouldNotBeNull();
        requestPath.ShouldBe(expectedPath);
    }

    [Fact]
    public async Task SaveContent_sends_upsert_request_to_keyed_endpoint()
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
                  "variants":[{"language":1,"title":"Welcome","body":"Ride with us","requiresHumanReview":false},{"language":2,"title":"Bem-vindo","body":"Pedale conosco","requiresHumanReview":true}],
                  "publicationState":"ReviewRequired"
                }
                """);
        });
        var sut = new PublicContentApiClient(httpClient);
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco", RequiresHumanReview = true });

        // Act
        var saved = await sut.SaveContent("/home//hero/", request, Xunit.TestContext.Current.CancellationToken);

        // Assert
        requestMethod.ShouldBe("PUT");
        requestPath.ShouldBe("/catalog/public-content/home/hero");
        saved.PublicationState.ShouldBe("ReviewRequired");
    }

    [Fact]
    public async Task SaveContent_throws_api_validation_exception_when_server_returns_validation_problem()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
        {
            var problem = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(PublicContentVariantDto.Title)] = ["Title is required."]
            });

            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(problem)
            };
        });
        var sut = new PublicContentApiClient(httpClient);
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = string.Empty, Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco" });

        // Act
        Func<Task> act = () => sut.SaveContent("home.hero", request, Xunit.TestContext.Current.CancellationToken);

        var exception = await act.ShouldThrow<ContractValidationException>();

        // Assert
        exception.ValidationErrors.Keys.ShouldContain(nameof(PublicContentVariantDto.Title));
    }

}
