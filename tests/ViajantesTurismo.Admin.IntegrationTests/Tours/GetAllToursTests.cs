using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class GetAllToursTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Tours()
    {
        // Arrange
        // Act
        var response = await Client.GetAsync(new Uri("/tours", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tours = await response.Content.ReadFromJsonAsync<GetTourDto[]>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(tours);
        Assert.NotEmpty(tours);
    }
}
