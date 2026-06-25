using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogEndpointTests
{
    [Fact]
    public async Task Catalog_Tour_List_Returns_All_Tours()
    {
        // Arrange
        var store = new StubCatalogTourReadModelStore(CatalogEndpointTestsHelpers.CreateTour("TOUR-002", "Dolomites"));
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var tours = await response.Content.ReadFromJsonAsync<CatalogTourDto[]>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tours);
        var tour = Assert.Single(tours);
        Assert.Equal("Dolomites", tour.Title);
        Assert.Equal("TOUR-002", tour.Slug);
        Assert.False(tour.IsPublished);
    }

    [Fact]
    public async Task Public_Tour_List_Returns_Empty_List_When_No_Tours_Are_Published()
    {
        // Arrange
        var store = new StubCatalogTourReadModelStore(CatalogEndpointTestsHelpers.CreateTour("TOUR-002", "Dolomites"));
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var tours = await response.Content.ReadFromJsonAsync<CatalogTourDto[]>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tours);
        Assert.Empty(tours);
    }

    [Fact]
    public async Task Public_Tour_Details_Returns_NotFound_When_Tour_Is_Not_Published()
    {
        // Arrange
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(new StubCatalogTourReadModelStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/tours/missing-tour", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Public_Tour_Details_Returns_BadRequest_For_Whitespace_Slug()
    {
        // Arrange
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(new StubCatalogTourReadModelStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/tours/%20", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    internal sealed class StubCatalogTourReadModelStore(params CatalogTourDraftReadModel[] tours) : ICatalogTourReadModelStore
    {
        public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
        {
            IReadOnlyList<CatalogTourDraftReadModel> snapshot = tours;
            return ValueTask.FromResult(snapshot);
        }

        public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
        {
            return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
        }
    }

    internal static class CatalogEndpointTestsHelpers
    {
        public static WebApplicationFactory<ICatalogApiAssemblyMarker> CreateFactory(ICatalogTourReadModelStore store)
        {
            return WebApplicationTestHost.Create<ICatalogApiAssemblyMarker>(
                configureTestServices: services =>
                {
                    services.RemoveAll<ICatalogTourReadModelStore>();
                    services.AddSingleton(store);
                });
        }

        public static CatalogTourDraftReadModel CreateTour(string identifier, string title)
        {
            return new CatalogTourDraftReadModel(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                identifier,
                title,
                1,
                DateTimeOffset.UtcNow);
        }
    }
}
