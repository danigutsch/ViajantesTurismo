using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiEndpointTests
{
    [Fact]
    public void Catalog_api_marker_exposes_entry_assembly()
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
    public async Task Default_health_endpoint_returns_success(string path)
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
    public async Task Production_default_health_endpoint_is_not_exposed(string path)
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
    public async Task Public_content_endpoint_saves_review_required_draft()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco", RequiresHumanReview = true });

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<PublicContentDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("HOME.HERO", saved.Key);
        Assert.Contains(saved.Variants, variant => variant.Language == PublicContentLanguageDto.PtBr && variant.RequiresHumanReview);
        Assert.Equal("ReviewRequired", saved.PublicationState);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_body_is_invalid()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = string.Empty, Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco" });

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicContentVariantDto.Title), problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_variant_language_is_duplicated()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco" });

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Variants", problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_variants_is_null()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        using var content = new StringContent(
            """
            { "sourceLanguage": 1, "variants": null }
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(UpsertPublicContentRequest.Variants), problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_variant_element_is_null()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        using var content = new StringContent(
            """
            {
              "sourceLanguage": 1,
              "variants": [
                null,
                { "language": 2, "title": "Bem-vindo", "body": "Pedale conosco" }
              ]
            }
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(UpsertPublicContentRequest.Variants), problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_supported_language_is_missing()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Variants", problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_read_endpoint_returns_requested_approved_variant()
    {
        // Arrange
        var publicContentStore = new TestPublicContentStore();
        var enUs = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            "Welcome",
            "Ride with us",
            "Cycle tours",
            null,
            null,
            requiresHumanReview: false);
        var ptBr = PublicContentVariant.Create(
            PublicContentLanguage.PtBr,
            "Bem-vindo",
            "Pedale conosco",
            "Cicloturismo",
            null,
            null,
            requiresHumanReview: false);
        Assert.True(enUs.IsSuccess);
        Assert.True(ptBr.IsSuccess);
        var content = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        Assert.True(content.IsSuccess);
        var publish = content.Value.Publish();
        Assert.True(publish.IsSuccess);
        await publicContentStore.SaveContent(content.Value, TestContext.Current.CancellationToken);

        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/content/home.hero?culture=pt-BR", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(variant);
        Assert.Equal(PublicContentLanguageDto.PtBr, variant.Language);
        Assert.Equal("Bem-vindo", variant.Title);
        Assert.False(variant.RequiresHumanReview);
    }

    [Fact]
    public async Task Public_content_read_endpoint_falls_back_to_approved_english_variant()
    {
        // Arrange
        var publicContentStore = new TestPublicContentStore();
        var enUs = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            "Welcome",
            "Ride with us",
            null,
            null,
            null,
            requiresHumanReview: false);
        var ptBr = PublicContentVariant.Create(
            PublicContentLanguage.PtBr,
            "Bem-vindo",
            "Pedale conosco",
            null,
            null,
            null,
            requiresHumanReview: true);
        Assert.True(enUs.IsSuccess);
        Assert.True(ptBr.IsSuccess);
        var content = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        Assert.True(content.IsSuccess);
        var publicationState = typeof(EditablePublicContent).GetProperty(nameof(EditablePublicContent.PublicationState));
        Assert.NotNull(publicationState);
        publicationState.SetValue(content.Value, PublicContentPublicationState.Published);
        await publicContentStore.SaveContent(content.Value, TestContext.Current.CancellationToken);

        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/content/home.hero?culture=pt-BR", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(variant);
        Assert.Equal(PublicContentLanguageDto.EnUs, variant.Language);
        Assert.Equal("Welcome", variant.Title);
        Assert.False(variant.RequiresHumanReview);
    }

    [Fact]
    public async Task Catalog_tour_presentation_endpoint_returns_validation_problem_when_values_are_too_long()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertCatalogTourPresentationRequest
        {
            Title = new string('t', ContractConstants.MaxNameLength + 1),
            Slug = new string('s', ContractConstants.MaxSlugLength + 1),
            IsPublished = true
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/catalog/tours/{Guid.CreateVersion7()}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(UpsertCatalogTourPresentationRequest.Title), problem.Errors.Keys);
        Assert.Contains(nameof(UpsertCatalogTourPresentationRequest.Slug), problem.Errors.Keys);
    }
}
