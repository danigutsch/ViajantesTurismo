using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class CreateTourTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Tour()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new CreateTourDto
        {
            Identifier = $"CUBA-{uniqueId}",
            Name = "Isla de Cuba",
            StartDate = new DateTime(2024, 10, 10).ToUniversalTime(),
            EndDate = new DateTime(2024, 10, 20).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 2500.00m,
            DoubleRoomSupplementPrice = 300.00m,
            RegularBikePrice = 150.00m,
            EBikePrice = 250.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            IncludedServices = ["Hotel", "Breakfast", "City Tour"]
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request,
            TestContext.Current.CancellationToken);

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
        const string invalidIdentifier = "";
        var request = new CreateTourDto
        {
            Identifier = invalidIdentifier,
            Name = "Test Tour",
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7),
            Currency = CurrencyDto.Real,
            Price = 2000.00m,
            DoubleRoomSupplementPrice = 500.00m,
            RegularBikePrice = 100.00m,
            EBikePrice = 200.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            IncludedServices = ["Hotel", "Breakfast"]
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Tour_Returns_Validation_Problem_For_Multiple_Errors()
    {
        // Arrange
        const decimal invalidPrice = 0.00m;
        var request = new CreateTourDto
        {
            Identifier = "TEST2024",
            Name = "Test Tour",
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7),
            Currency = CurrencyDto.Real,
            Price = invalidPrice,
            DoubleRoomSupplementPrice = invalidPrice,
            RegularBikePrice = invalidPrice,
            EBikePrice = invalidPrice,
            MinCustomers = 4,
            MaxCustomers = 12,
            IncludedServices = ["Hotel"]
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Tour_Returns_Bad_Request_For_Invalid_Price()
    {
        // Arrange
        const decimal negativeBasePrice = -100.00m;
        var request = new CreateTourDto
        {
            Identifier = "TEST2024",
            Name = "Test Tour",
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7),
            Currency = CurrencyDto.Real,
            Price = negativeBasePrice,
            DoubleRoomSupplementPrice = 500.00m,
            RegularBikePrice = 100.00m,
            EBikePrice = 200.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            IncludedServices = ["Hotel", "Breakfast"]
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("price", content, StringComparison.OrdinalIgnoreCase);
    }
}
