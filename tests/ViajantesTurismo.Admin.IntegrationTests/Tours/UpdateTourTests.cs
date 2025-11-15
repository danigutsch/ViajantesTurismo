using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class UpdateTourTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Update_Tour()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new CreateTourDto
        {
            Identifier = $"CUBA-{uniqueId}",
            Name = "Isla de Cuba",
            StartDate = new DateTime(2026, 10, 10).ToUniversalTime(),
            EndDate = new DateTime(2026, 10, 20).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 2700.00m,
            DoubleRoomSupplementPrice = 360.00m,
            RegularBikePrice = 170.00m,
            EBikePrice = 270.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            IncludedServices = ["Hotel", "Breakfast", "City Tour"]
        };
        var postResponse = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var location = postResponse.Headers.Location;
        Assert.NotNull(location);
        var idStr = location.OriginalString.Split('/').Last();
        Assert.True(Guid.TryParse(idStr, out var tourId));

        var updateRequest = new UpdateTourDto
        {
            Identifier = $"CUBA-{uniqueId}-UPDATED",
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
        var putResponse =
            await Client.PutAsJsonAsync($"/tours/{tourId}", updateRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // Assert
        var getResponse = await Client.GetAsync(new Uri($"/tours/{tourId}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var tourDto =
            await getResponse.Content.ReadFromJsonAsync<GetTourDto>(
                cancellationToken: TestContext.Current.CancellationToken);
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
        var response = await Client.PutAsJsonAsync($"/tours/{invalidId}", updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
