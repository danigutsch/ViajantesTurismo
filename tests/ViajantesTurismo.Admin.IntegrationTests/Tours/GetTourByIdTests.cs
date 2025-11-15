using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class GetTourByIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Tour_By_Id()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAsync(new Uri($"/tours/{tour.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tourDto = await response.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(tourDto);
        Assert.Equal(tour.Id, tourDto.Id);
        Assert.Equal(tour.Identifier, tourDto.Identifier);
    }

    [Fact]
    public async Task Get_Tour_By_Id_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = -1;

        // Act
        var response = await Client.GetAsync(new Uri($"/tours/{invalidId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
