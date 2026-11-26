using Microsoft.AspNetCore.TestHost;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Gallery;

/// <summary>Verifies the exact Admin-only reverse lookup of an original Gallery wake.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[TestHost(TestHost.TestServer)]
public sealed class CatalogGalleryOriginalWakeLookupTests
{
    [Theory]
    [InlineData("Admin", false, true, HttpStatusCode.OK)]
    [InlineData("Admin", false, false, HttpStatusCode.NotFound)]
    [InlineData("Admin", true, false, HttpStatusCode.BadRequest)]
    [InlineData(null, false, true, HttpStatusCode.Unauthorized)]
    [InlineData("Operator", false, true, HttpStatusCode.Forbidden)]
    public async Task Original_wake_lookup_returns_only_the_exact_authorized_placement(
        string? role, bool emptyWakeId, bool found, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var wakeId = emptyWakeId ? Guid.Empty : Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var placement = found
            ? new GetCatalogGalleryPlacementStatusDto(tourId, 7, wakeId, CatalogGalleryPlacementStatusDto.Pending, null, null, null)
            : null;
        var query = new StubCatalogGalleryOperationsQuery(placement);
        await using var baseline = CatalogApiTestHost.CreateAnonymous();
        await using var factory = baseline.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<ICatalogGalleryOperationsQuery>(query)));
        using var client = factory.CreateClient();
        if (role is not null)
        {
            CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        var route = new Uri($"/api/v1/catalog/operations/gallery/original-wakes/{wakeId:D}/placement", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.OriginalWakeRequest.ShouldBe(expected is HttpStatusCode.OK or HttpStatusCode.NotFound ? wakeId : null);
        query.Request.ShouldBeNull();
        if (expected == HttpStatusCode.OK)
        {
            var returned = await response.Content.ReadFromJsonAsync<GetCatalogGalleryPlacementStatusDto>(ct);
            returned.ShouldBe(placement);
        }
    }
}
