using System.Net;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.ApiService;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogEndpointTests
{
    [Fact]
    public async Task Public_Tour_Details_Returns_BadRequest_For_Whitespace_Slug()
    {
        // Arrange
        await using var factory = WebApplicationTestHost.Create<ICatalogApiAssemblyMarker>();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/tours/%20", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
