using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class CreateTourTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Tour()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateTourDto(
            basePrice: 2500.00m,
            doubleRoomSupplement: 300.00m,
            regularBikePrice: 150.00m,
            eBikePrice: 250.00m,
            currency: CurrencyDto.Real,
            includedServices: ["Hotel", "Breakfast", "City Tour"]);

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tour = await response.Content.ReadFromJsonAsync<GetTourDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(tour);
        Assert.Equal(request.Identifier, tour.Identifier);
    }

    [Fact]
    public async Task Create_Tour_Returns_Bad_Request_For_Invalid_Data()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateTourDto(
            identifier: "",
            name: "Test Tour");

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Tour_Returns_Validation_Problem_For_Multiple_Errors()
    {
        // Arrange
        const decimal invalidPrice = 0.00m;
        var request = DtoBuilders.BuildCreateTourDto(
            identifier: "TEST2024",
            name: "Test Tour",
            basePrice: invalidPrice,
            doubleRoomSupplement: invalidPrice,
            regularBikePrice: invalidPrice,
            eBikePrice: invalidPrice,
            includedServices: ["Hotel"]);

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Tour_Returns_Bad_Request_For_Invalid_Price()
    {
        // Arrange
        const decimal negativeBasePrice = -100.00m;
        var request = DtoBuilders.BuildCreateTourDto(
            identifier: "TEST2024",
            name: "Test Tour",
            basePrice: negativeBasePrice);

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("price", content, StringComparison.OrdinalIgnoreCase);
    }
}
