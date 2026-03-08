using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

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

        var createdIds = new HashSet<Guid>
        {
            tour1.Id,
            tour2.Id,
            tour3.Id,
        };

        var createdTours = tours.Where(t => createdIds.Contains(t.Id)).ToArray();

        Assert.Equal(3, createdTours.Length);
        Assert.Contains(createdTours, t => t.Id == tour1.Id);
        Assert.Contains(createdTours, t => t.Id == tour2.Id);
        Assert.Contains(createdTours, t => t.Id == tour3.Id);
    }
}

public sealed class GetAllToursEmptyListTests(ApiFixture fixture) : AdminApiSerialTestBase(fixture)
{
    [Fact]
    [Trait("SeedDependency", "Intentional-EmptyState-Smoke")]
    public async Task Can_Get_Empty_Tour_List()
    {
        // Arrange
        await ClearDatabaseAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAllToursAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tours = await response.Content.ReadFromJsonAsync<GetTourDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(tours);
        Assert.Empty(tours);
    }
}
