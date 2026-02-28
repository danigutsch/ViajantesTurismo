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
            singleRoomSupplement: 300.00m,
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
            singleRoomSupplement: invalidPrice,
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

    [Fact]
    public async Task Cannot_Create_Tour_With_Duplicate_Identifier()
    {
        // Arrange
        var duplicateIdentifier = TestDataGenerator.UniqueTourIdentifier("DUP");
        var firstRequest = DtoBuilders.BuildCreateTourDto(identifier: duplicateIdentifier, name: "First Tour");
        var secondRequest = DtoBuilders.BuildCreateTourDto(identifier: duplicateIdentifier, name: "Second Tour");

        await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), firstRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), secondRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("already exists", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_Tour_Accepts_Maximum_Valid_Price()
    {
        // Arrange
        const decimal maxPrice = 100_000.00m;
        var request = DtoBuilders.BuildCreateTourDto(
            identifier: "MAXPRICE",
            name: "Max Price Tour",
            basePrice: maxPrice,
            singleRoomSupplement: maxPrice,
            regularBikePrice: maxPrice,
            eBikePrice: maxPrice);

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_Tour_Accepts_Minimum_Valid_Price()
    {
        // Arrange
        const decimal minPrice = 0.01m;
        var request = DtoBuilders.BuildCreateTourDto(
            identifier: "MINPRICE",
            name: "Min Price Tour",
            basePrice: minPrice,
            singleRoomSupplement: minPrice,
            regularBikePrice: minPrice,
            eBikePrice: minPrice);

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
