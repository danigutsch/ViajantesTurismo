using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class GetAllToursTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Multiple_Tours()
    {
        // Arrange
        var tour1 = await Client.CreateTestTour(identifier: "SUM001", name: "Summer Adventure", cancellationToken: TestContext.Current.CancellationToken);
        var tour2 = await Client.CreateTestTour(identifier: "WIN001", name: "Winter Escape", cancellationToken: TestContext.Current.CancellationToken);
        var tour3 = await Client.CreateTestTour(identifier: "SPR001", name: "Spring Journey", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAllToursAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tours = await response.Content.ReadFromJsonAsync<GetTourDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(tours);
        Assert.True(tours.Length >= 3);
        Assert.Contains(tours, t => t.Id == tour1.Id);
        Assert.Contains(tours, t => t.Id == tour2.Id);
        Assert.Contains(tours, t => t.Id == tour3.Id);
    }
}
