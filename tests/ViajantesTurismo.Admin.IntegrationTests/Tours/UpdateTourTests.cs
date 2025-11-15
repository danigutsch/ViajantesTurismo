using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class UpdateTourTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Update_Tour()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);

        var updateRequest = new UpdateTourDto
        {
            Identifier = $"{tour.Identifier}-UPDATED",
            Name = "Cuba Updated",
            StartDate = new DateTime(2026, 11, 10).ToUniversalTime(),
            EndDate = new DateTime(2026, 11, 20).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 2800.00m,
            DoubleRoomSupplementPrice = 370.00m,
            RegularBikePrice = 180.00m,
            EBikePrice = 280.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            IncludedServices = ["Hotel", "Breakfast", "City Tour", "Dinner"]
        };

        // Act
        var putResponse = await Client.PutAsJsonAsync($"/tours/{tour.Id}", updateRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // Assert
        var getResponse = await Client.GetAsync(new Uri($"/tours/{tour.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var tourDto = await getResponse.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(tourDto);
        Assert.Equal(updateRequest.Identifier, tourDto.Identifier);
        Assert.Equal(updateRequest.Name, tourDto.Name);
        Assert.Equal(updateRequest.Price, tourDto.Price);
        Assert.Equal(updateRequest.Currency, tourDto.Currency);
        Assert.Equal(updateRequest.IncludedServices, tourDto.IncludedServices);
    }

    [Fact]
    public async Task Update_Tour_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = -1;

        var updateRequest = new UpdateTourDto
        {
            Identifier = "INVALID",
            Name = "Invalid Tour",
            StartDate = new DateTime(2027, 1, 1).ToUniversalTime(),
            EndDate = new DateTime(2027, 1, 10).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 1000.00m,
            DoubleRoomSupplementPrice = 100.00m,
            RegularBikePrice = 50.00m,
            EBikePrice = 80.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            IncludedServices = ["None"]
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/tours/{invalidId}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
