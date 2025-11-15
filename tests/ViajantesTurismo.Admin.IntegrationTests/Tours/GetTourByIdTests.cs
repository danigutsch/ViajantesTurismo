using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Tours;

public sealed class GetTourByIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Tour_By_Id()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new CreateTourDto
        {
            Identifier = $"CUBA-{uniqueId}",
            Name = "Isla de Cuba",
            StartDate = new DateTime(2025, 10, 10).ToUniversalTime(),
            EndDate = new DateTime(2025, 10, 20).ToUniversalTime(),
            Currency = CurrencyDto.Real,
            Price = 2600.00m,
            DoubleRoomSupplementPrice = 350.00m,
            RegularBikePrice = 160.00m,
            EBikePrice = 260.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            IncludedServices = ["Hotel", "Breakfast", "City Tour"]
        };
        var createResponse = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        // Act
        var response = await Client.GetAsync(location, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tourDto = await response.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(tourDto);
        Assert.Equal(request.Identifier, tourDto.Identifier);
        Assert.Equal(request.Name, tourDto.Name);
        Assert.Equal(request.Price, tourDto.Price);
        Assert.Equal(request.Currency, tourDto.Currency);
        Assert.Equal(request.IncludedServices, tourDto.IncludedServices);
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
